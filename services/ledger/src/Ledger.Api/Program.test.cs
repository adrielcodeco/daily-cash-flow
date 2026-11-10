// @anchors
//   code: LDHTS
//   ref: LDHTP
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Api/Program.feature
//   spec    : services/ledger/src/Ledger.Api/Program.spec.md
using System.Net;
using System.Text;
using static Ledger.Tests.LedgerHttp;

namespace Ledger.Tests;

public sealed class ProgramTests(LedgerApiFactory api) : IClassFixture<LedgerApiFactory>
{
    private readonly HttpClient client = api.CreateClient();

    [Fact(DisplayName = "LDHTP-R01 a domain refusal answers in the problem format with its code")]
    public async Task AnswersDomainRefusals()
    {
        var token = Merchant(Guid.NewGuid());
        var future = new { entryType = "DEBIT", amountCents = 10, accountingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)) };

        await AssertProblem(await client.Send(HttpMethod.Post, "/v1/entries", token, NewKey(), future), HttpStatusCode.UnprocessableEntity, "future_accounting_date");
        await AssertProblem(await client.Send(HttpMethod.Get, $"{ListPath}&limit=500", token), HttpStatusCode.BadRequest, "invalid_limit");
    }

    [Fact(DisplayName = "LDHTP-R02 a refusal without a body of its own answers in the problem format")]
    public async Task AnswersBodilessRefusals()
    {
        var token = Merchant(Guid.NewGuid());
        var malformed = new StringContent("{ not json", Encoding.UTF8, "application/json");

        await AssertProblem(await client.Send(HttpMethod.Post, "/v1/entries", token, NewKey(), malformed), HttpStatusCode.BadRequest);
        await AssertProblem(await client.Send(HttpMethod.Get, "/v1/entries?date=not-a-date", token), HttpStatusCode.BadRequest);
        await AssertProblem(await client.Send(HttpMethod.Get, ListPath), HttpStatusCode.Unauthorized);
        await AssertProblem(await client.Send(HttpMethod.Get, ListPath, Merchant(Guid.NewGuid(), "entries.write")), HttpStatusCode.Forbidden);
    }
}
