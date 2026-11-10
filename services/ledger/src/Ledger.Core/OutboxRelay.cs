// @anchors
//   code: OBRCD
//   ref: OBRLY
//   updated_at: 2026-11-09
namespace Ledger.Core;

public sealed record OutboxMessage(Guid Id, string Type, string Payload, string? TraceContext = null);

public interface IOutbox
{
    Task<IOutboxBatch> ClaimPendingAsync(int limit, CancellationToken cancellationToken);
}

public interface IOutboxBatch : IAsyncDisposable
{
    IReadOnlyList<OutboxMessage> Messages { get; }

    Task MarkPublishedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Puts the event behind the others for a while, longer after each failure.</summary>
    Task DeferAsync(Guid id, CancellationToken cancellationToken);

    Task CommitAsync(CancellationToken cancellationToken);
}

public interface IEventPublisher
{
    /// <summary>Completes only after the broker confirms the message.</summary>
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken);
}

public sealed class OutboxRelay(IOutbox outbox, IEventPublisher publisher)
{
    public async Task<int> PublishPendingAsync(int limit, CancellationToken cancellationToken)
    {
        await using var batch = await outbox.ClaimPendingAsync(limit, cancellationToken);
        var confirmed = new List<Guid>();
        Exception? failure = null;

        try
        {
            foreach (var message in batch.Messages)
            {
                try
                {
                    await publisher.PublishAsync(message, cancellationToken);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    failure = exception;
                    // A message that keeps failing must not hold back the ones behind it.
                    await batch.DeferAsync(message.Id, CancellationToken.None);
                    break;
                }

                confirmed.Add(message.Id);
            }
        }
        finally
        {
            // What the broker confirmed stays marked, even when the publisher is stopping.
            await batch.MarkPublishedAsync(confirmed, CancellationToken.None);
            await batch.CommitAsync(CancellationToken.None);
        }

        if (failure is not null)
        {
            throw new OutboxPublishException(confirmed.Count, failure);
        }

        return confirmed.Count;
    }
}

public sealed class OutboxPublishException(int published, Exception inner)
    : Exception("Publishing stopped after a broker failure.", inner)
{
    public int Published { get; } = published;
}
