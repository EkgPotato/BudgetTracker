using Finance.Api.Data;
using Finance.Api.Features.Imports;
using Finance.Api.Features;
using Finance.Api.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<FinanceDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("FinanceDb")));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? throw new InvalidOperationException("Brak konfiguracji Jwt.");
builder.Services.AddIdentityCore<AppUser>(options => { options.User.RequireUniqueEmail = true; options.Password.RequiredLength = 8; options.Password.RequireNonAlphanumeric = false; })
    .AddEntityFrameworkStores<FinanceDbContext>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true, ValidIssuer = jwt.Issuer, ValidateAudience = true, ValidAudience = jwt.Audience,
    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
    ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1)
});
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddOpenApi();
builder.Services.AddScoped<ExcelBudgetParser>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapAuthEndpoints();
app.MapFinanceEndpoints();
app.MapExcelImportEndpoints();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    await MarkEnsureCreatedDatabaseAsBaselineAsync(database);
    await database.Database.MigrateAsync();
}
app.Run();

static async Task MarkEnsureCreatedDatabaseAsBaselineAsync(FinanceDbContext database)
{
    // Early local versions used EnsureCreated(), which creates tables but no EF migration history.
    // Preserve that data by recording the initial migration before normal migrations are applied.
    if (!await database.Database.CanConnectAsync()) return;

    var initialMigration = database.Database.GetMigrations().FirstOrDefault()
        ?? throw new InvalidOperationException("Nie znaleziono migracji początkowej.");

    await database.Database.ExecuteSqlRawAsync("IF OBJECT_ID(N'__EFMigrationsHistory', N'U') IS NULL CREATE TABLE [__EFMigrationsHistory] ([MigrationId] nvarchar(150) NOT NULL, [ProductVersion] nvarchar(32) NOT NULL, CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]));");
    var requiresBaseline = await database.Database
        .SqlQuery<int>($"SELECT CAST(CASE WHEN OBJECT_ID(N'AspNetRoles', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = {initialMigration}) THEN 1 ELSE 0 END AS int) AS [Value]")
        .SingleAsync();

    if (requiresBaseline == 0) return;

    await database.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ({initialMigration}, {"10.0.0"});");
}
