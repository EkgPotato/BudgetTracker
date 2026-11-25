using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Finance.Api.Configuration;
using Finance.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Finance.Api.Managers;

public sealed class JwtTokenManager(IOptions<JwtOptions> options) : IJwtTokenManager
{
    public string Create(FinanceUser user)
    {
        var configuration = options.Value;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(ClaimTypes.SerialNumber, user.SecurityStamp ?? string.Empty)
        };
        if (user.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            configuration.Issuer,
            configuration.Audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(Math.Clamp(configuration.AccessTokenLifetimeMinutes, 5, 60)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
