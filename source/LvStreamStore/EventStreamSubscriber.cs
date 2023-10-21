namespace LvStreamStore {
    using System;
    using System.Threading.Tasks;

    internal sealed class EventStreamSubscriber : IHandle<RecordedEvent>, IHandle<UpdateSubscription>, IDisposable {
        private readonly Bus _bus;
        private readonly Func<RecordedEvent, Task> _onAppeared;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private Func<RecordedEvent, bool>? _handleFilter;
        private IDisposable _busSubscription;
        private IDisposable _updateSubscription;

        internal EventStreamSubscriber(Bus bus, Func<RecordedEvent, Task> onAppeared) {
            _bus = bus;
            _onAppeared = onAppeared;
        }

        public IDisposable Start(StreamId streamId) {
            _handleFilter = new Func<RecordedEvent, bool>((@event) => streamId == @event.StreamId);
            _busSubscription = _bus.Subscribe<RecordedEvent>(this);
            _updateSubscription = _bus.Subscribe<UpdateSubscription>(this);
            return this;
        }

        public IDisposable Start(StreamKey streamKey) {
            _handleFilter = new Func<RecordedEvent, bool>((@event) => streamKey == @event.StreamId);
            _busSubscription = _bus.Subscribe<RecordedEvent>(this);
            _updateSubscription = _bus.Subscribe<UpdateSubscription>(this);
            return this;
        }

        public void Handle(RecordedEvent message) {
            if (_handleFilter?.Invoke(message) ?? false) {
                _onAppeared.Invoke(message);
            }
        }

        public void Handle(UpdateSubscription message) {
            //todo: iterate reader and bring to current.
            //EventStreamReader esr;
            //var eventsToProcess = AsyncHelper.RunSync(async () => await esr.ToListAsync());
            //foreach (var e in eventsToProcess.OfType<RecordedEvent>()) {
            //    Handle(new EventRecorded(e));
            //}
        }

        public void Dispose() {
            _cts?.Dispose();
            _busSubscription?.Dispose();
            _updateSubscription?.Dispose();
        }
    }

    public record UpdateSubscription : Event;
}