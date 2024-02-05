namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading.Tasks;

    public interface IStreamStoreRepository {
        ValueTask<bool> Save<TAggregate>(TAggregate aggregate) where TAggregate : AggregateRoot, new();
        ValueTask<TAggregate> TryGetById<TAggregate>(Guid aggregateId) where TAggregate : AggregateRoot, new();

        /// <summary>
        /// Listens for particular events.
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="handle"></param>
        /// <returns></returns>
        IDisposable Subscribe<TEvent>(Messaging.IReceiver<TEvent> handle) where TEvent : Event;

        /// <summary>
        /// Reads all events for a particular stream key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        IAsyncEnumerable<Event> ReadAsync(StreamKey key);

        /// <summary>
        /// Reads all events for a particular aggregate type.
        /// </summary>
        /// <typeparam name="TAggregate"></typeparam>
        /// <returns></returns>
        IAsyncEnumerable<Event> ReadAsync<TAggregate>() where TAggregate : AggregateRoot, new();
    }
}
