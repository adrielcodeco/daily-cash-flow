// @anchors
//   code: LDEPT
//   layer: host
//   updated_at: 2026-11-06
using Ledger.Core;
using Microsoft.AspNetCore.Mvc;

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

public sealed record EntryPageResponse(IReadOnlyList<EntryResponse> Items, Guid? NextCursor);

public static class EntryEndpoints
{
    private const int MinKeyLength = 8;
    private const int MaxKeyLength = 128;

    public static void MapEntryEndpoints(this IEndpointRouteBuilder app)
    {
        var entries = app.MapGroup("/v1/entries");

        entries.MapPost("/", async (
            NewEntryRequest request,
            HttpContext context,
            [FromHeader(Name = "Idempotency-Key")] string? key,
            RecordEntry recordEntry,
            CancellationToken cancellationToken) =>
        {
            if (!MerchantContext.TryRead(context, out var merchantId))
            {
                return UnknownMerchant();
            }

            if (!IsValidKey(key))
            {
                return InvalidKey();
            }

            var entry = await recordEntry.ExecuteAsync(
                new RecordEntryCommand(merchantId, request.EntryType, request.AmountCents, request.AccountingDate, request.Description),
                key!,
                cancellationToken);

            return Results.Created($"/v1/entries/{entry.Id}", EntryResponse.From(entry));
        }).RequireAuthorization(ApiSecurity.EntriesWrite);

        entries.MapGet("/", async (
            DateOnly date,
            Guid? cursor,
            int? limit,
            HttpContext context,
            ListEntries listEntries,
            CancellationToken cancellationToken) =>
        {
            if (!MerchantContext.TryRead(context, out var merchantId))
            {
                return UnknownMerchant();
            }

            var page = await listEntries.ExecuteAsync(merchantId, date, cursor, limit ?? ListEntries.DefaultLimit, cancellationToken);
            return Results.Ok(new EntryPageResponse(page.Items.Select(EntryResponse.From).ToList(), page.NextCursor));
        }).RequireAuthorization(ApiSecurity.EntriesRead);

        entries.MapGet("/{id:guid}", async (Guid id, HttpContext context, IEntryStore store, CancellationToken cancellationToken) =>
        {
            if (!MerchantContext.TryRead(context, out var merchantId))
            {
                return UnknownMerchant();
            }

            var entry = await store.FindAsync(merchantId, id, cancellationToken);
            return entry is null
                ? Results.Problem(title: "The entry was not found.", statusCode: StatusCodes.Status404NotFound, type: "entry_not_found")
                : Results.Ok(EntryResponse.From(entry));
        }).RequireAuthorization(ApiSecurity.EntriesRead);

        entries.MapPost("/{id:guid}/reversal", async (
            Guid id,
            HttpContext context,
            [FromHeader(Name = "Idempotency-Key")] string? key,
            ReverseEntry reverseEntry,
            CancellationToken cancellationToken) =>
        {
            if (!MerchantContext.TryRead(context, out var merchantId))
            {
                return UnknownMerchant();
            }

            if (!IsValidKey(key))
            {
                return InvalidKey();
            }

            var reversal = await reverseEntry.ExecuteAsync(merchantId, id, key!, cancellationToken);
            return Results.Created($"/v1/entries/{reversal.Id}", EntryResponse.From(reversal));
        }).RequireAuthorization(ApiSecurity.EntriesWrite);
    }

    private static bool IsValidKey(string? key) => key?.Length is >= MinKeyLength and <= MaxKeyLength;

    private static IResult InvalidKey() =>
        Results.Problem(title: "The Idempotency-Key header must have 8 to 128 characters.", statusCode: StatusCodes.Status400BadRequest, type: "invalid_idempotency_key");

    private static IResult UnknownMerchant() =>
        Results.Problem(title: "The merchant could not be identified.", statusCode: StatusCodes.Status401Unauthorized);
}
