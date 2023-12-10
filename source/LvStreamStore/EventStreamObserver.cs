namespace LvStreamStore {
    using System;
    using System.Collections.Generic;
    using System.Reactive;
    using System.Threading.Tasks;

    internal class EventStreamObserver : IObserver<Unit>, IDisposable {
        private readonly EventStream _eventStream;
        private readonly EventStreamReader _reader;
        private readonly CancellationTokenSource _cts = new();
        private CancellationTokenSource _onTick = new();
        private Dictionary<StreamKey, List<IHandleAsync<StreamItem>>> _handlers;
        private EventBus _bus = new(null);

        public EventStreamObserver(EventStream eventStream) {
            _eventStream = eventStream;

            _reader = _eventStream.GetReader();
            _cts.Token.Register(() => _onTick?.Cancel());
            _ = Task.Run(OnTick, _cts.Token);
        }

        public IDisposable ObserveForEvents(IHandleAsync<StreamItem> handler) {
            var subscription = _bus.Subscribe(new AdHocHandler<StreamItem>(handler.HandleAsync));

            return new Disposer(() => subscription?.Dispose());
        }

        public IDisposable ObserveForEvents(StreamId @id, IHandleAsync<StreamItem> handler) {
            var subscription = _bus.Subscribe(new AdHocHandler<StreamItem>(async (item) => {
                if (id == item.StreamId) {
                    await handler.HandleAsync(item);
                }
            }));
            return new Disposer(() => subscription?.Dispose());
        }

        public IDisposable ObserveForEvents(StreamKey key, IHandleAsync<StreamItem> handler) {
            if (!_handlers.TryGetValue(key, out var handlers)) {
                handlers = new List<IHandleAsync<StreamItem>>();
                _handlers.Add(key, handlers);
            }

            handlers.Add(handler);

            return new Disposer(() => {
                handlers.Remove(handler);
            });
        }

        public void OnCompleted() {
            // throw new NotImplementedException();
        }

        public void OnError(Exception error) {
            // throw new NotImplementedException();
        }

        public void OnNext(Unit value) {
            _onTick?.Cancel();
        }

        private async Task OnTick() {
            while (!_cts.IsCancellationRequested) {

                await foreach (var item in _reader) {

                }

                await Task.Delay(250, _onTick.Token);
                _onTick?.Dispose();
                _onTick = new CancellationTokenSource();
            }
        }

        public void Dispose() {
            _cts?.Cancel();
        }
    }
}
