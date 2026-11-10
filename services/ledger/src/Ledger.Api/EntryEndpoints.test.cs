// @anchors
//   code: LDEPX
//   ref: LDEPS
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Api/EntryEndpoints.feature
//   spec    : services/ledger/src/Ledger.Api/EntryEndpoints.spec.md
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static Ledger.Tests.LedgerHttp;

namespace Ledger.Tests;

public sealed class EntryEndpointsTests(LedgerApiFactory api) : IClassFixture<LedgerApiFactory>
{
    private readonly HttpClient client = api.CreateClient();

    [Theory(DisplayName = "LDEPS-R01 a write without an Idempotency-Key of 8 to 128 characters is refused")]
    [InlineData(null)]
    [InlineData(7)]
    [InlineData(129)]
    public async Task RefusesInvalidKey(int? length)
    {
        var merchant = Guid.NewGuid();
        var key = length is null ? null : new string('k', length.Value);
        var original = await client.RecordFor(merchant);

        var record = await client.Send(HttpMethod.Post, "/v1/entries", Merchant(merchant), key, Credit());
        var reverse = await client.Send(HttpMethod.Post, $"/v1/entries/{original}/reversal", Merchant(merchant), key);

        await AssertProblem(record, HttpStatusCode.BadRequest, "invalid_idempotency_key");
        await AssertProblem(reverse, HttpStatusCode.BadRequest, "invalid_idempotency_key");
        Assert.Single(api.Store.Entries, entry => entry.MerchantId == merchant);
    }

    [Theory(DisplayName = "LDEPS-R01 the limits of the Idempotency-Key length are accepted")]
    [InlineData(8)]
    [InlineData(128)]
    public async Task AcceptsKeyLengthLimits(int length)
    {
        var key = NewKey().PadRight(length, 'k')[..length];

        var response = await client.Send(HttpMethod.Post, "/v1/entries", Merchant(Guid.NewGuid()), key, Credit());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory(DisplayName = "LDEPS-R02 an entry type sent as a number is refused")]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(7)]
    public async Task RefusesNumericEntryType(int entryType)
    {
        var merchant = Guid.NewGuid();
        var body = new { entryType, amountCents = 1500, accountingDate = Yesterday };

        var response = await client.Send(HttpMethod.Post, "/v1/entries", Merchant(merchant), NewKey(), body);

        await AssertProblem(response, HttpStatusCode.BadRequest);
        Assert.DoesNotContain(api.Store.Entries, entry => entry.MerchantId == merchant);
    }

    [Fact(DisplayName = "LDEPS-R03 an entry of another merchant answers 404")]
    public async Task HidesOtherMerchantEntry()
    {
        var owner = Guid.NewGuid();
        var id = await client.RecordFor(owner);
        var intruder = Merchant(Guid.NewGuid());

        var read = await client.Send(HttpMethod.Get, $"/v1/entries/{id}", intruder);
        var reverse = await client.Send(HttpMethod.Post, $"/v1/entries/{id}/reversal", intruder, NewKey());

        await AssertProblem(read, HttpStatusCode.NotFound, "entry_not_found");
        await AssertProblem(reverse, HttpStatusCode.NotFound);
        Assert.Single(api.Store.Entries, entry => entry.MerchantId == owner);
    }

    [Fact(DisplayName = "LDEPS-R03 the list of another merchant does not show the entry")]
    public async Task ListsOnlyOwnEntries()
    {
        await client.RecordFor(Guid.NewGuid());

        var response = await client.Send(HttpMethod.Get, ListPath, Merchant(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("items").GetArrayLength());
    }

    [Theory(DisplayName = "LDEPS-R04 a merchant that cannot be identified is refused with 401")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    public async Task RefusesUnknownMerchant(string merchant)
    {
        var before = api.Store.Entries.Count;
        var token = LedgerApiFactory.Token(merchant, ReadAndWrite);

        await AssertProblem(await client.Send(HttpMethod.Post, "/v1/entries", token, NewKey(), Credit()), HttpStatusCode.Unauthorized);
        await AssertProblem(await client.Send(HttpMethod.Get, ListPath, token), HttpStatusCode.Unauthorized);
        await AssertProblem(await client.Send(HttpMethod.Get, $"/v1/entries/{Guid.NewGuid()}", token), HttpStatusCode.Unauthorized);
        await AssertProblem(await client.Send(HttpMethod.Post, $"/v1/entries/{Guid.NewGuid()}/reversal", token, NewKey()), HttpStatusCode.Unauthorized);
        Assert.Equal(before, api.Store.Entries.Count);
    }
}
