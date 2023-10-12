namespace LvStreamStore {
    using System;
    using System.Threading.Tasks;

    internal sealed class EventStreamSubscriber : IHandle<EventRecorded>, IDisposable {
        private readonly InMemoryBus _bus;
        private readonly Func<RecordedEvent, Task> _onAppeared;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Func<EventRecorded, bool>? _handleFilter;
        private IDisposable _busSubscription;

        internal EventStreamSubscriber(InMemoryBus bus, Func<RecordedEvent, Task> onAppeared) {
            _bus = bus;
            _onAppeared = onAppeared;
        }

        public IDisposable Start(StreamId streamId) {
            _handleFilter = new Func<EventRecorded, bool>((@event) => streamId == @event.Event.StreamId);
            _busSubscription = _bus.Subscribe(this);
            return this;
        }

        public IDisposable Start(StreamKey streamKey) {
            _handleFilter = new Func<EventRecorded, bool>((@event) => streamKey == @event.Event.StreamId);
            _busSubscription = _bus.Subscribe(this);
            return this;
        }

        public void Handle(EventRecorded message) {
            if (_handleFilter?.Invoke(message) ?? false) {
                _onAppeared.Invoke(message.Event);
            }
        }

        public void Dispose() {
            _cts?.Dispose();
            _busSubscription?.Dispose();
        }
    }
}