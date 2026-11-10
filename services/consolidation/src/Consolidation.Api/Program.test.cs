// @anchors
//   code: CNHTS
//   ref: CNHTP
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Api/Program.feature
//   spec    : services/consolidation/src/Consolidation.Api/Program.spec.md
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Consolidation.Core;
using static Consolidation.Tests.ConsolidationHttp;

namespace Consolidation.Tests;

public sealed class ProgramTests(ConsolidationApiFactory api) : IClassFixture<ConsolidationApiFactory>
{
    private readonly HttpClient client = api.CreateClient();

    [Theory(DisplayName = "CNHTP-R01 a merchant that cannot be identified is refused with 401")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    public async Task RefusesUnknownMerchant(string merchant)
    {
        var daily = api.Store.DailyQueries;
        var period = api.Store.PeriodQueries;
        var token = ConsolidationApiFactory.Token(merchant, BalancesRead);

        await AssertProblem(await client.Get(Daily, token), HttpStatusCode.Unauthorized);
        await AssertProblem(await client.Get(Period, token), HttpStatusCode.Unauthorized);
        Assert.Equal(daily, api.Store.DailyQueries);
        Assert.Equal(period, api.Store.PeriodQueries);
    }

    [Fact(DisplayName = "CNHTP-R02 the balance is always the one of the merchant in the token")]
    public async Task ShowsOnlyOwnBalance()
    {
        var owner = Guid.NewGuid();
        await api.Store.ApplyOnceAsync(new BalanceChange(Guid.NewGuid(), owner, Day, 1500, 0, DateTimeOffset.UtcNow), CancellationToken.None);

        var own = await (await client.Get(Daily, Merchant(owner))).Content.ReadFromJsonAsync<JsonElement>();
        var other = await (await client.Get(Daily, Merchant(Guid.NewGuid()))).Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1500, own.GetProperty("creditsCents").GetInt64());
        Assert.Equal(0, other.GetProperty("creditsCents").GetInt64());
    }

    [Fact(DisplayName = "CNHTP-R03 a refusal of the query answers in the problem format with its code")]
    public async Task AnswersQueryRefusals()
    {
        var token = Merchant(Guid.NewGuid());

        await AssertProblem(await client.Get($"/v1/balances/period?start={Day:yyyy-MM-dd}&end={Day.AddDays(-1):yyyy-MM-dd}", token), HttpStatusCode.BadRequest, "invalid_period");
        await AssertProblem(await client.Get($"/v1/balances/period?start={Day.AddDays(-400):yyyy-MM-dd}&end={Day:yyyy-MM-dd}", token), HttpStatusCode.BadRequest, "period_too_long");
    }

    [Fact(DisplayName = "CNHTP-R04 a refusal without a body of its own answers in the problem format")]
    public async Task AnswersBodilessRefusals()
    {
        var token = Merchant(Guid.NewGuid());

        await AssertProblem(await client.Get("/v1/balances/daily?date=not-a-date", token), HttpStatusCode.BadRequest);
        await AssertProblem(await client.Get("/v1/balances/daily", token), HttpStatusCode.BadRequest);
        await AssertProblem(await client.Get(Daily), HttpStatusCode.Unauthorized);
        await AssertProblem(await client.Get(Daily, Merchant(Guid.NewGuid(), "entries.read")), HttpStatusCode.Forbidden);
    }
}
