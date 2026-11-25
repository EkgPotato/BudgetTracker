using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Claims;
using Finance.Api.Configuration;
using Finance.Api.Features;
using Finance.Api.Managers;
using Finance.Api.Services;
using Finance.Api.Validators;
using Finance.Infrastructure.Data;
using Finance.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<FinanceDbContext>(options =>
        options.UseInMemoryDatabase("Finance.TestMode"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("FinanceDb");
    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException(
            "Configure ConnectionStrings:FinanceDb using user secrets or environment variables.");

    builder.Services.AddDbContext<FinanceDbContext>(options =>
        options.UseSqlServer(connectionString));
}
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MailpitOptions>(builder.Configuration.GetSection("Mailpit"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ??
          throw new InvalidOperationException("Brak konfiguracji Jwt.");
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Configure Jwt:Key with a secret of at least 32 characters.");
builder.Services.AddIdentityCore<FinanceUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<FinanceDbContext>()
    .AddDefaultTokenProviders();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue("sub");
                var securityStamp = context.Principal?.FindFirstValue(ClaimTypes.SerialNumber);
                if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(securityStamp))
                {
                    context.Fail("Nieprawidłowy token.");
                    return;
                }

                var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<FinanceUser>>();
                var user = await users.FindByIdAsync(userId);
                if (user is null || !string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal))
                    context.Fail("Token został unieważniony.");
            }
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddOpenApi();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<IAuthManager, AuthManager>();
builder.Services.AddScoped<IJwtTokenManager, JwtTokenManager>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IFinanceDataService, FinanceDataService>();
builder.Services.AddScoped<IFinanceManager, FinanceManager>();
builder.Services.AddSingleton<AuthRequestValidator>();
builder.Services.AddSingleton<FinanceRequestValidator>();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();
if (app.Environment.IsEnvironment("Testing"))
    await TestDataSeeder.SeedAsync(app.Services, app.Configuration);
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapControllers();
app.Run();

public partial class Program;
