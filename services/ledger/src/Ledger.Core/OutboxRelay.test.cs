// @anchors
//   code: OBRTS
//   ref: OBRLY
//   updated_at: 2026-11-09
//   layer: test
// Refs:
//   feature : services/ledger/src/Ledger.Core/OutboxRelay.feature
//   spec    : services/ledger/src/Ledger.Core/OutboxRelay.spec.md
using Ledger.Core;

namespace Ledger.Tests;

public sealed class OutboxRelayTests
{
    private static OutboxMessage[] Pending(int count) =>
        Enumerable.Range(0, count).Select(_ => new OutboxMessage(Guid.NewGuid(), EntryRecorded.EventType, "{}")).ToArray();

    [Fact(DisplayName = "OBRLY-R01 confirmed events are marked as published")]
    public async Task MarksConfirmedEvents()
    {
        var messages = Pending(2);
        var outbox = new FakeOutbox(messages);

        var published = await new OutboxRelay(outbox, new FakePublisher()).PublishPendingAsync(10, CancellationToken.None);

        Assert.Equal(2, published);
        Assert.Equal(messages.Select(m => m.Id), outbox.Marked);
        Assert.True(outbox.Committed);
    }

    [Fact(DisplayName = "OBRLY-R02 a failure stops the batch")]
    public async Task StopsOnFailure()
    {
        var messages = Pending(3);
        var publisher = new FakePublisher(failOnCall: 2);

        await Assert.ThrowsAsync<OutboxPublishException>(() =>
            new OutboxRelay(new FakeOutbox(messages), publisher).PublishPendingAsync(10, CancellationToken.None));

        Assert.Equal(2, publisher.Calls);
    }

    [Fact(DisplayName = "OBRLY-R03 events confirmed before the failure stay marked")]
    public async Task KeepsEventsConfirmedBeforeFailure()
    {
        var messages = Pending(3);
        var outbox = new FakeOutbox(messages);

        await Assert.ThrowsAsync<OutboxPublishException>(() =>
            new OutboxRelay(outbox, new FakePublisher(failOnCall: 2)).PublishPendingAsync(10, CancellationToken.None));

        Assert.Equal([messages[0].Id], outbox.Marked);
        Assert.True(outbox.Committed);
    }

    [Fact(DisplayName = "OBRLY-R03 events confirmed before a shutdown stay marked")]
    public async Task KeepsEventsConfirmedBeforeShutdown()
    {
        var messages = Pending(3);
        var outbox = new FakeOutbox(messages);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new OutboxRelay(outbox, new FakePublisher(failOnCall: 2, cancel: true)).PublishPendingAsync(10, CancellationToken.None));

        Assert.Equal([messages[0].Id], outbox.Marked);
        Assert.True(outbox.Committed);
    }

    [Fact(DisplayName = "OBRLY-R04 the failed event is deferred")]
    public async Task DefersFailedEvent()
    {
        var messages = Pending(3);
        var outbox = new FakeOutbox(messages);

        await Assert.ThrowsAsync<OutboxPublishException>(() =>
            new OutboxRelay(outbox, new FakePublisher(failOnCall: 2)).PublishPendingAsync(10, CancellationToken.None));

        Assert.Equal([messages[1].Id], outbox.Deferred);
    }

    private sealed class FakeOutbox(IReadOnlyList<OutboxMessage> messages) : IOutbox, IOutboxBatch
    {
        public List<Guid> Marked { get; } = [];

        public List<Guid> Deferred { get; } = [];

        public bool Committed { get; private set; }

        public IReadOnlyList<OutboxMessage> Messages => messages;

        public Task<IOutboxBatch> ClaimPendingAsync(int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IOutboxBatch>(this);

        public Task MarkPublishedAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
        {
            Marked.AddRange(ids);
            return Task.CompletedTask;
        }

        public Task DeferAsync(Guid id, CancellationToken cancellationToken)
        {
            Deferred.Add(id);
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakePublisher(int? failOnCall = null, bool cancel = false) : IEventPublisher
    {
        public int Calls { get; private set; }

        public Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls != failOnCall)
            {
                return Task.CompletedTask;
            }

            return cancel ? throw new OperationCanceledException() : throw new InvalidOperationException("Broker unavailable");
        }
    }
}
