namespace LvStreamStore;

using Microsoft.Extensions.Logging;

public partial class EventStream {
    class Subscribers : IDisposable {
        EventStream _stream;
        EventStreamReader _reader;
        CancellationTokenSource _cts = new();
        Bus _bus;
        List<IDisposable> _disposables = new();

        internal Subscribers(EventStream stream, ILogger logger) {
            _stream = stream;
            _reader = _stream.GetReader();
            _bus = new Bus(logger);
            Task.Factory.StartNew(ReadEventsAsyc, _cts.Token);
        }

        public async Task<IDisposable> SubscribeToStreamAsync(Func<RecordedEvent, Task> onAppeared) {
            var events = _stream.GetReader().OfType<RecordedEvent>();

            await foreach (var item in events) {
                await onAppeared(item);
            }

            var disposable = _bus.Subscribe(new AdHocHandler<RecordedEvent>(onAppeared));
            var disposer = new Disposer(() => {
                disposable.Dispose();
                _disposables.Remove(disposable);
            });
            return disposer;
        }


        private async Task ReadEventsAsyc() {
            while (!_cts.IsCancellationRequested) {
                await foreach (var item in _reader) {
                    if (item is RecordedEvent @event) {
                        await _bus.PublishAsync(@event);
                    }
                }

                await Task.Delay(50);
            }
        }

        public void Dispose() {
            _bus?.Dispose();
            _cts?.Dispose();
            foreach (var d in _disposables ?? new List<IDisposable>()) {
                d?.Dispose();
            }
        }
    }
}