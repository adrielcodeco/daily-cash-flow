// @anchors
//   code: CNADT
//   ref: CNADS
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/consolidation/src/Consolidation.Api/AuditMiddleware.feature
//   spec    : services/consolidation/src/Consolidation.Api/AuditMiddleware.spec.md
using System.Net;
using static Consolidation.Tests.ConsolidationHttp;

namespace Consolidation.Tests;

public sealed class AuditMiddlewareTests
{
    [Fact(DisplayName = "CNADS-R01 a refused access is recorded in the audit trail")]
    public async Task AuditsRefusedAccess()
    {
        await using var api = new ConsolidationApiFactory();
        var client = api.CreateClient();
        var forbidden = Guid.NewGuid();

        await client.Get(Daily);
        await client.Get(Daily, Merchant(forbidden, "entries.read"));

        Assert.True(await api.Logs.WaitForAsync(line => line.Contains("GET /v1/balances/daily answered 401")));
        Assert.True(await api.Logs.WaitForAsync(line => line.Contains("GET /v1/balances/daily answered 403") && line.Contains(forbidden.ToString()) && line.Contains("test-client")));
    }

    [Fact(DisplayName = "CNADS-R01 a request refused by the rate limit is recorded in the audit trail")]
    public async Task AuditsRateLimited()
    {
        await using var api = new ConsolidationApiFactory { PermitsPerSecond = 1 };
        var client = api.CreateClient();
        var merchant = Guid.NewGuid();

        var refused = await UntilLimited(() => client.Get(Daily, Merchant(merchant)));

        Assert.NotNull(refused);
        Assert.True(await api.Logs.WaitForAsync(line => line.Contains("GET /v1/balances/daily answered 429") && line.Contains(merchant.ToString())));
    }

    [Fact(DisplayName = "CNADS-R02 a successful query stays out of the audit trail")]
    public async Task SkipsSuccessfulQuery()
    {
        await using var api = new ConsolidationApiFactory();
        var client = api.CreateClient();
        var merchant = Guid.NewGuid();

        var response = await client.Get(Daily, Merchant(merchant));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await api.Logs.WaitForAsync(line => line.Contains(merchant.ToString())));
    }
}
