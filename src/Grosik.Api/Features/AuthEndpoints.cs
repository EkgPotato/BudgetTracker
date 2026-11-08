using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finance.Api.Contracts;
using Finance.Api.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Finance.Api.Features;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
}

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");
        group.MapPost("/register", async (RegisterRequest request, HttpContext context, UserManager<AppUser> users, IOptions<JwtOptions> options) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["E-mail i hasło są wymagane."] });

            var anyUsers = await users.Users.AnyAsync();
            if (anyUsers && !(context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Admin"))) return Results.Forbid();
            var user = new AppUser { UserName = request.Email.Trim(), Email = request.Email.Trim(), EmailConfirmed = true, IsAdmin = !anyUsers };
            var result = await users.CreateAsync(user, request.Password);
            if (!result.Succeeded) return Results.ValidationProblem(result.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(x => x.Description).ToArray()));
            return Results.Created($"/api/auth/users/{user.Id}", new { user.Id, user.Email, user.IsAdmin, accessToken = CreateToken(user, options.Value) });
        }).AllowAnonymous();
        group.MapPost("/login", async (LoginRequest request, UserManager<AppUser> users, IOptions<JwtOptions> options) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["credentials"] = ["E-mail i hasło są wymagane."] });
            var user = await users.FindByEmailAsync(request.Email.Trim());
            if (user is null || !await users.CheckPasswordAsync(user, request.Password)) return Results.Unauthorized();
            return Results.Ok(new { accessToken = CreateToken(user, options.Value), user = new { user.Id, user.Email, user.IsAdmin } });
        }).AllowAnonymous();
        group.MapGet("/me", (ClaimsPrincipal principal) => Results.Ok(new
        {
            id = principal.FindFirstValue(JwtRegisteredClaimNames.Sub),
            email = principal.FindFirstValue(JwtRegisteredClaimNames.Email),
            isAdmin = principal.IsInRole("Admin")
        })).RequireAuthorization();
        return app;
    }

    private static string CreateToken(AppUser user, JwtOptions options)
    {
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty), new(ClaimTypes.Name, user.UserName ?? string.Empty) };
        if (user.IsAdmin) claims.Add(new(ClaimTypes.Role, "Admin"));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key));
        var token = new JwtSecurityToken(options.Issuer, options.Audience, claims, expires: DateTime.UtcNow.AddHours(12), signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
