namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading.Tasks;

    public interface IStreamStoreRepository {
        ValueTask<bool> Save<TAggregate>(TAggregate aggregate) where TAggregate : AggregateRoot, new();
        ValueTask<TAggregate> TryGetById<TAggregate>(Guid aggregateId) where TAggregate : AggregateRoot, new();
        IDisposable Subscribe<TAggregate, TEvent>(IAsyncHandler<TEvent> handle) where TAggregate : AggregateRoot, new() where TEvent : Event;
        IAsyncEnumerable<Message> ReadAsync(StreamKey key);
    }
}
