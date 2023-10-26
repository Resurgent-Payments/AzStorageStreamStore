namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading.Tasks;

    public interface IStreamStoreRepository {
        public ValueTask<bool> Save<TAggregate>(TAggregate aggregate) where TAggregate : AggregateRoot, new();
        public ValueTask<TAggregate> TryGetById<TAggregate>(Guid aggregateId) where TAggregate : AggregateRoot, new();
    }
}
