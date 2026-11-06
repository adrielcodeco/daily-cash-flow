// @anchors
//   code: LDEPT
//   layer: host
//   updated_at: 2026-11-05
using Ledger.Core;

namespace Ledger.Api;

public sealed record NewEntryRequest(EntryType EntryType, long AmountCents, DateOnly AccountingDate, string? Description);

public sealed record EntryResponse(
    Guid Id,
    EntryType EntryType,
    long AmountCents,
    DateOnly AccountingDate,
    string? Description,
    Guid? ReversalOf,
    DateTimeOffset CreatedAt)
{
    public static EntryResponse From(LedgerEntry entry) => new(
        entry.Id, entry.EntryType, entry.AmountCents, entry.AccountingDate, entry.Description, entry.ReversalOf, entry.CreatedAt);
}

public static class EntryEndpoints
{
    public static void MapEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var entries = app.MapGroup("/v1/entries");

        entries.MapPost("/", async (NewEntryRequest request, HttpContext context, RecordEntry recordEntry, CancellationToken cancellationToken) =>
        {
            if (!MerchantContext.TryRead(context, out var merchantId))
            {
                return Results.Problem(title: "The merchant could not be identified.", statusCode: StatusCodes.Status401Unauthorized);
            }

            var entry = await recordEntry.ExecuteAsync(
                new RecordEntryCommand(merchantId, request.EntryType, request.AmountCents, request.AccountingDate, request.Description),
                cancellationToken);

            return Results.Created($"/v1/entries/{entry.Id}", EntryResponse.From(entry));
        });

        entries.MapGet("/{id:guid}", async (Guid id, HttpContext context, IEntryStore store, CancellationToken cancellationToken) =>
        {
            if (!MerchantContext.TryRead(context, out var merchantId))
            {
                return Results.Problem(title: "The merchant could not be identified.", statusCode: StatusCodes.Status401Unauthorized);
            }

            var entry = await store.FindAsync(merchantId, id, cancellationToken);
            return entry is null
                ? Results.Problem(title: "The entry was not found.", statusCode: StatusCodes.Status404NotFound)
                : Results.Ok(EntryResponse.From(entry));
        });
    }
}
