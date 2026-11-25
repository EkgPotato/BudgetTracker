using System.Net;
using System.Net.Http.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Finance.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Finance.Tests;

public sealed class ApiContractTests : IClassFixture<ApiContractTests.TestApiFactory>
{
    private readonly TestApiFactory factory;

    public ApiContractTests(TestApiFactory factory)
    {
        this.factory = factory;
    }

    [Fact]
    public async Task Dashboard_requires_authentication()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_with_missing_credentials_returns_validation_problem()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new { email = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Login_denies_user_with_unconfirmed_email()
    {
        var email = $"unconfirmed-{Guid.NewGuid():N}@example.test";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<FinanceUser>>();
            var result = await users.CreateAsync(new FinanceUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = false
            }, "IntegrationPass123!");
            Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
        }

        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "IntegrationPass123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        public TestApiFactory()
        {
            Environment.SetEnvironmentVariable(
                "Jwt__Key",
                "integration-test-jwt-key-with-at-least-32-characters");
            Environment.SetEnvironmentVariable("Testing__DemoPassword", "IntegrationPass123!");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
        }
    }
}
