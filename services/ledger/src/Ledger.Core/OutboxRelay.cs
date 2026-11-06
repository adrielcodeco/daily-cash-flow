// @anchors
//   code: OBRCD
//   ref: OBRLY
//   updated_at: 2026-11-05
namespace Ledger.Core;

public sealed record OutboxMessage(Guid Id, string Type, string Payload);

public interface IOutbox
{
    Task<IOutboxBatch> ClaimPendingAsync(int limit, CancellationToken cancellationToken);
}

public interface IOutboxBatch : IAsyncDisposable
{
    IReadOnlyList<OutboxMessage> Messages { get; }

    Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken);

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
        var published = 0;
        Exception? failure = null;

        foreach (var message in batch.Messages)
        {
            try
            {
                await publisher.PublishAsync(message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                failure = exception;
                break;
            }

            await batch.MarkPublishedAsync(message.Id, cancellationToken);
            published++;
        }

        await batch.CommitAsync(cancellationToken);

        if (failure is not null)
        {
            throw new OutboxPublishException(published, failure);
        }

        return published;
    }
}

public sealed class OutboxPublishException(int published, Exception inner)
    : Exception("Publishing stopped after a broker failure.", inner)
{
    public int Published { get; } = published;
}
