using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Finance.Tests;

public sealed class TransactionPageContractTests : IClassFixture<ApiContractTests.TestApiFactory>
{
    private readonly ApiContractTests.TestApiFactory factory;

    public TransactionPageContractTests(ApiContractTests.TestApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Page_endpoint_requires_authentication_and_validates_bounds()
    {
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/transactions/page")).StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "demo@grosik.test", password = "IntegrationPass123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("accessToken").GetString());

        Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/transactions/page?limit=101")).StatusCode);
        var response = await client.GetAsync("/api/transactions/page?limit=1&kinds=expense");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var page = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(page.RootElement.GetProperty("total").GetInt32() > 1);
        Assert.Equal(1, page.RootElement.GetProperty("items").GetArrayLength());
        Assert.Equal("expense", page.RootElement.GetProperty("items")[0].GetProperty("kind").GetString());
    }
}
