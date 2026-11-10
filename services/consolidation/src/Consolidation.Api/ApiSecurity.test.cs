// @anchors
//   code: CNSCT
//   ref: CNSCP
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Api/ApiSecurity.feature
//   spec    : services/consolidation/src/Consolidation.Api/ApiSecurity.spec.md
using System.Net;
using static Consolidation.Tests.ConsolidationHttp;

namespace Consolidation.Tests;

public sealed class ApiSecurityTests(ConsolidationApiFactory api) : IClassFixture<ConsolidationApiFactory>
{
    private readonly HttpClient client = api.CreateClient();

    public static TheoryData<string> Queries() => new() { Daily, Period };

    [Theory(DisplayName = "CNSCP-R01 a request without a token is refused with 401")]
    [MemberData(nameof(Queries))]
    public async Task RefusesMissingToken(string path)
    {
        var response = await client.Get(path);

        await AssertProblem(response, HttpStatusCode.Unauthorized);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
    }

    [Fact(DisplayName = "CNSCP-R01 a token issued for another audience is refused with 401")]
    public async Task RefusesForeignAudience()
    {
        var token = ConsolidationApiFactory.Token(Guid.NewGuid().ToString(), BalancesRead, audience: "another-api");

        await AssertProblem(await client.Get(Daily, token), HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "CNSCP-R02 a query without the balances.read scope is refused with 403")]
    [InlineData("entries.read")]
    [InlineData("entries.read entries.write")]
    [InlineData("")]
    public async Task RefusesMissingScope(string scope)
    {
        var token = Merchant(Guid.NewGuid(), scope);

        await AssertProblem(await client.Get(Daily, token), HttpStatusCode.Forbidden);
        await AssertProblem(await client.Get(Period, token), HttpStatusCode.Forbidden);
    }

    [Fact(DisplayName = "CNSCP-R02 the balances.read scope opens both queries")]
    public async Task AcceptsScope()
    {
        var token = Merchant(Guid.NewGuid());

        Assert.Equal(HttpStatusCode.OK, (await client.Get(Daily, token)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.Get(Period, token)).StatusCode);
    }

    [Fact(DisplayName = "CNSCP-R03 a token without the merchant is refused with 403")]
    public async Task RefusesTokenWithoutMerchant()
    {
        var daily = api.Store.DailyQueries;
        var period = api.Store.PeriodQueries;
        var token = ConsolidationApiFactory.Token(null, BalancesRead);

        await AssertProblem(await client.Get(Daily, token), HttpStatusCode.Forbidden);
        await AssertProblem(await client.Get(Period, token), HttpStatusCode.Forbidden);
        Assert.Equal(daily, api.Store.DailyQueries);
        Assert.Equal(period, api.Store.PeriodQueries);
    }

    [Fact(DisplayName = "CNSCP-R04 a merchant named like the health check is limited like any other")]
    public async Task LimitsMerchantNamedHealth()
    {
        await using var limited = new ConsolidationApiFactory { PermitsPerSecond = 3 };
        var http = limited.CreateClient();
        var token = ConsolidationApiFactory.Token("health", BalancesRead);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/live")).StatusCode);

        var refused = await UntilLimited(() => http.Get(Daily, token));

        Assert.NotNull(refused);
    }

    [Fact(DisplayName = "CNSCP-R04 the limit is counted per merchant and spares the health check")]
    public async Task LimitsPerMerchant()
    {
        await using var limited = new ConsolidationApiFactory { PermitsPerSecond = 2 };
        var http = limited.CreateClient();
        var busy = Merchant(Guid.NewGuid());

        var refused = await UntilLimited(() => http.Get(Daily, busy));
        var other = await http.Get(Daily, Merchant(Guid.NewGuid()));
        var health = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            health.Add((await http.GetAsync("/health/live")).StatusCode);
        }

        Assert.NotNull(refused);
        await AssertProblem(refused, HttpStatusCode.TooManyRequests, "rate_limited");
        Assert.Equal("1", refused.Headers.RetryAfter?.ToString());
        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        Assert.All(health, status => Assert.Equal(HttpStatusCode.OK, status));
    }
}
