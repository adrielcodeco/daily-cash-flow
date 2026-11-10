// @anchors
//   code: LDADT
//   ref: LDADS
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Api/AuditMiddleware.feature
//   spec    : services/ledger/src/Ledger.Api/AuditMiddleware.spec.md
using System.Net;
using static Ledger.Tests.LedgerHttp;

namespace Ledger.Tests;

public sealed class AuditMiddlewareTests(LedgerApiFactory api) : IClassFixture<LedgerApiFactory>
{
    private readonly HttpClient client = api.CreateClient();

    [Fact(DisplayName = "LDADS-R01 a write is recorded in the audit trail")]
    public async Task AuditsWrite()
    {
        var merchant = Guid.NewGuid();

        var id = await client.RecordFor(merchant);

        Assert.True(await api.Logs.WaitForAsync(line => line.Contains("POST /v1/entries answered 201") && line.Contains(merchant.ToString()) && line.Contains("test-client")));
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact(DisplayName = "LDADS-R02 a read of another merchant's entry is recorded in the audit trail")]
    public async Task AuditsNotFound()
    {
        var id = await client.RecordFor(Guid.NewGuid());
        var intruder = Guid.NewGuid();

        await client.Send(HttpMethod.Get, $"/v1/entries/{id}", Merchant(intruder));

        Assert.True(await api.Logs.WaitForAsync(line => line.Contains($"GET /v1/entries/{id} answered 404") && line.Contains(intruder.ToString())));
    }

    [Fact(DisplayName = "LDADS-R02 a refused access is recorded in the audit trail")]
    public async Task AuditsRefusedAccess()
    {
        var unauthenticated = Guid.NewGuid();
        var forbidden = Guid.NewGuid();

        await client.Send(HttpMethod.Get, $"/v1/entries/{unauthenticated}");
        await client.Send(HttpMethod.Get, $"/v1/entries/{forbidden}", Merchant(Guid.NewGuid(), "entries.write"));

        Assert.True(await api.Logs.WaitForAsync(line => line.Contains($"GET /v1/entries/{unauthenticated} answered 401")));
        Assert.True(await api.Logs.WaitForAsync(line => line.Contains($"GET /v1/entries/{forbidden} answered 403")));
    }

    [Fact(DisplayName = "LDADS-R02 a request refused by the rate limit is recorded in the audit trail")]
    public async Task AuditsRateLimited()
    {
        await using var limited = new LedgerApiFactory { PermitsPerSecond = 1 };
        var http = limited.CreateClient();
        var merchant = Guid.NewGuid();

        var refused = await UntilLimited(() => http.Send(HttpMethod.Get, ListPath, Merchant(merchant)));

        Assert.NotNull(refused);
        Assert.True(await limited.Logs.WaitForAsync(line => line.Contains("GET /v1/entries answered 429") && line.Contains(merchant.ToString())));
    }

    [Fact(DisplayName = "LDADS-R03 a successful read stays out of the audit trail")]
    public async Task SkipsSuccessfulRead()
    {
        var merchant = Guid.NewGuid();
        var id = await client.RecordFor(merchant);

        var read = await client.Send(HttpMethod.Get, $"/v1/entries/{id}", Merchant(merchant));

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.False(await api.Logs.WaitForAsync(line => line.Contains($"GET /v1/entries/{id} answered")));
    }
}
