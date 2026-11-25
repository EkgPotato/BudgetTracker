using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Finance.Api.Configuration;
using Finance.Api.Managers;
using Finance.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace Finance.Tests;

public sealed class JwtTokenManagerTests
{
    [Fact]
    public void Create_IncludesSecurityStampAndUsesConfiguredShortLifetime()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "issuer",
            Audience = "audience",
            Key = "01234567890123456789012345678901",
            AccessTokenLifetimeMinutes = 15
        });
        var user = new FinanceUser
        {
            Id = "user-id",
            Email = "user@example.test",
            UserName = "user@example.test",
            SecurityStamp = "security-stamp"
        };

        var token = new JwtSecurityTokenHandler().ReadJwtToken(new JwtTokenManager(options).Create(user));

        Assert.Equal("user-id", token.Subject);
        Assert.Equal("security-stamp", token.Claims.Single(claim => claim.Type == ClaimTypes.SerialNumber).Value);
        Assert.InRange(token.ValidTo, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }
}
