// @anchors
//   code: LDSCT
//   ref: LDSCP
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Api/ApiSecurity.feature
//   spec    : services/ledger/src/Ledger.Api/ApiSecurity.spec.md
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using static Ledger.Tests.LedgerHttp;

namespace Ledger.Tests;

public sealed class ApiSecurityTests(LedgerApiFactory api) : IClassFixture<LedgerApiFactory>
{
    private readonly HttpClient client = api.CreateClient();

    public static TheoryData<string, string> Operations() => new()
    {
        { "POST", "/v1/entries" },
        { "GET", ListPath },
        { "GET", $"/v1/entries/{Guid.NewGuid()}" },
        { "POST", $"/v1/entries/{Guid.NewGuid()}/reversal" },
    };

    [Theory(DisplayName = "LDSCP-R01 a request without a token is refused with 401")]
    [MemberData(nameof(Operations))]
    public async Task RefusesMissingToken(string method, string path)
    {
        var before = api.Store.Entries.Count;

        var response = await client.Send(new HttpMethod(method), path, key: NewKey(), body: method == "POST" ? Credit() : null);

        await AssertProblem(response, HttpStatusCode.Unauthorized);
        Assert.Contains(response.Headers.WwwAuthenticate, header => header.Scheme == "Bearer");
        Assert.Equal(before, api.Store.Entries.Count);
    }

    [Fact(DisplayName = "LDSCP-R01 a token issued for another audience is refused with 401")]
    public async Task RefusesForeignAudience()
    {
        var token = LedgerApiFactory.Token(Guid.NewGuid().ToString(), ReadAndWrite, audience: "another-api");

        await AssertProblem(await client.Send(HttpMethod.Get, ListPath, token), HttpStatusCode.Unauthorized);
    }

    [Fact(DisplayName = "LDSCP-R01 a token signed by another key is refused with 401")]
    public async Task RefusesForeignSignature()
    {
        var token = LedgerApiFactory.Token(Guid.NewGuid().ToString(), ReadAndWrite, key: new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)));

        await AssertProblem(await client.Send(HttpMethod.Get, ListPath, token), HttpStatusCode.Unauthorized);
    }

    [Theory(DisplayName = "LDSCP-R02 an operation without its scope is refused with 403")]
    [InlineData("POST", "/v1/entries", "entries.read")]
    [InlineData("POST", "/v1/entries/{id}/reversal", "entries.read")]
    [InlineData("GET", "/v1/entries?date={date}", "entries.write")]
    [InlineData("GET", "/v1/entries/{id}", "entries.write")]
    [InlineData("GET", "/v1/entries/{id}", "")]
    public async Task RefusesMissingScope(string method, string path, string scope)
    {
        var merchant = Guid.NewGuid();
        var id = await client.RecordFor(merchant);
        var url = path.Replace("{id}", id.ToString()).Replace("{date}", Yesterday.ToString("yyyy-MM-dd"));

        var response = await client.Send(new HttpMethod(method), url, Merchant(merchant, scope), NewKey(), method == "POST" ? Credit() : null);

        await AssertProblem(response, HttpStatusCode.Forbidden);
        Assert.Single(api.Store.Entries, entry => entry.MerchantId == merchant);
    }

    [Fact(DisplayName = "LDSCP-R02 each scope opens its own operations")]
    public async Task AcceptsMatchingScope()
    {
        var merchant = Guid.NewGuid();
        var write = await client.Send(HttpMethod.Post, "/v1/entries", Merchant(merchant, "entries.write"), NewKey(), Credit());
        var id = (await write.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var read = await client.Send(HttpMethod.Get, $"/v1/entries/{id}", Merchant(merchant, "entries.read"));

        Assert.Equal(HttpStatusCode.Created, write.StatusCode);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
    }

    [Fact(DisplayName = "LDSCP-R03 a token without the merchant is refused with 403")]
    public async Task RefusesTokenWithoutMerchant()
    {
        var before = api.Store.Entries.Count;
        var token = LedgerApiFactory.Token(null, ReadAndWrite);

        await AssertProblem(await client.Send(HttpMethod.Post, "/v1/entries", token, NewKey(), Credit()), HttpStatusCode.Forbidden);
        await AssertProblem(await client.Send(HttpMethod.Get, ListPath, token), HttpStatusCode.Forbidden);
        Assert.Equal(before, api.Store.Entries.Count);
    }

    [Fact(DisplayName = "LDSCP-R04 a merchant named like the health check is limited like any other")]
    public async Task LimitsMerchantNamedHealth()
    {
        await using var limited = new LedgerApiFactory { PermitsPerSecond = 3 };
        var http = limited.CreateClient();
        var token = LedgerApiFactory.Token("health", ReadAndWrite);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/health/live")).StatusCode);

        var refused = await UntilLimited(() => http.Send(HttpMethod.Get, ListPath, token));

        Assert.NotNull(refused);
    }

    [Fact(DisplayName = "LDSCP-R04 the limit is counted per merchant and spares the health check")]
    public async Task LimitsPerMerchant()
    {
        await using var limited = new LedgerApiFactory { PermitsPerSecond = 2 };
        var http = limited.CreateClient();
        var busy = Merchant(Guid.NewGuid());
        var quiet = Merchant(Guid.NewGuid());

        var refused = await UntilLimited(() => http.Send(HttpMethod.Get, ListPath, busy));
        var other = await http.Send(HttpMethod.Get, ListPath, quiet);
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
