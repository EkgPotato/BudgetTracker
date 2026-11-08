using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finance.Api.Contracts;
using Finance.Api.Data;
using Finance.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Finance.Api.Features;

public sealed class JwtOptions { public string Issuer { get; init; } = string.Empty; public string Audience { get; init; } = string.Empty; public string Key { get; init; } = string.Empty; }
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/register", async (RegisterRequest request, UserManager<AppUser> users, SmtpEmailSender sender, IOptions<MailpitOptions> mailpit) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["E-mail i hasło są wymagane."] });
            var anyUsers = await users.Users.AnyAsync();
            var user = new AppUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), IsAdmin = !anyUsers };
            var result = await users.CreateAsync(user, request.Password); if (!result.Succeeded) return Results.ValidationProblem(result.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(x => x.Description).ToArray()));
            var token = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(await users.GenerateEmailConfirmationTokenAsync(user)));
            var link = $"{mailpit.Value.FrontendUrl}/auth?confirmUserId={Uri.EscapeDataString(user.Id)}&confirmToken={Uri.EscapeDataString(token)}";
            await sender.SendConfirmationAsync(user.Email!, link);
            return Results.Accepted($"/api/auth/users/{user.Id}", new { message = "Wysłaliśmy link potwierdzający na podany adres e-mail." });
        }).AllowAnonymous();
        group.MapPost("/confirm-email", async (ConfirmEmailRequest request, UserManager<AppUser> users) =>
        {
            var user = await users.FindByIdAsync(request.UserId); if (user is null) return Results.NotFound();
            var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token)); var result = await users.ConfirmEmailAsync(user, token);
            return result.Succeeded ? Results.Ok(new { message = "Adres e-mail został potwierdzony." }) : Results.BadRequest(new { message = "Link jest nieprawidłowy lub wygasł." });
        }).AllowAnonymous();
        group.MapPost("/login", async (LoginRequest request, UserManager<AppUser> users, IOptions<JwtOptions> options) =>
        {
            var user = await users.FindByEmailAsync(request.Email.Trim()); if (user is null || !await users.CheckPasswordAsync(user, request.Password)) return Results.Unauthorized();
            if (!user.EmailConfirmed) return Results.StatusCode(StatusCodes.Status403Forbidden); return Results.Ok(Response(user, options.Value));
        }).AllowAnonymous();
        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<AppUser> users) => { var user = await users.GetUserAsync(principal); return user is null ? Results.Unauthorized() : Results.Ok(new { user.Id, user.Email, user.IsAdmin, user.DisplayName, user.OnboardingCompleted }); }).RequireAuthorization();
        group.MapPut("/profile", async (UpdateProfileRequest request, ClaimsPrincipal principal, UserManager<AppUser> users, FinanceDbContext db) =>
        {
            var user = await users.GetUserAsync(principal);
            if (user is null) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(request.DisplayName)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["displayName"] = ["Podaj nazwę wyświetlaną."] });
            if (request.OnboardingCompleted && !await db.Accounts.AnyAsync(x => x.BudgetSpace!.OwnerId == user.Id)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["account"] = ["Utwórz pierwsze konto przed zakończeniem konfiguracji."] });
            user.DisplayName = request.DisplayName.Trim();
            user.OnboardingCompleted = request.OnboardingCompleted;
            var result = await users.UpdateAsync(user);
            return result.Succeeded ? Results.NoContent() : Results.ValidationProblem(result.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(x => x.Description).ToArray()));
        }).RequireAuthorization();
        return app;
    }
    private static object Response(AppUser user, JwtOptions options) => new { accessToken = Token(user, options), user = new { user.Id, user.Email, user.IsAdmin, user.DisplayName, user.OnboardingCompleted } };
    private static string Token(AppUser user, JwtOptions options) { var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty), new(ClaimTypes.Name, user.UserName ?? string.Empty) }; if (user.IsAdmin) claims.Add(new(ClaimTypes.Role, "Admin")); return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(options.Issuer, options.Audience, claims, expires: DateTime.UtcNow.AddHours(12), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)), SecurityAlgorithms.HmacSha256))); }
}
