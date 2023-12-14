namespace LvStreamStore.Subscriptions {
    using System;
    using System.Collections.Concurrent;

    using Microsoft.Extensions.Logging;

    internal class EventStreamPoller : IDisposable {
        private readonly CancellationTokenSource _cts = new();
        private readonly EventBus _bus;
        private readonly EventStreamPollerOptions _options;
        private ConcurrentBag<IDisposable> _subscriptions = new();
        EventStreamReader _streamReader;

        public EventStreamPoller(ILoggerFactory loggerFactory, EventStream stream, EventStreamPollerOptions options) {
            _bus = new EventBus(loggerFactory.CreateLogger<EventStreamPoller>());
            _streamReader = stream.GetReader();
            _options = options;

            // activate pump here?
            Task.Run(async () => {
                while (!_cts.IsCancellationRequested) {
                    await foreach (var msg in _streamReader) {
                        await _bus.PublishAsync(msg);
                    }

                    await Task.Delay(_options.PollingInterval).ConfigureAwait(false);
                }
            }, _cts.Token);
        }

        public IDisposable SubscribeToStream(IHandleAsync<StreamItem> handler) {
            var sub = _bus.Subscribe(handler);
            _subscriptions.Add(sub);
            return new Disposer(() => {
                Interlocked.Exchange(
                    ref _subscriptions,
                    new ConcurrentBag<IDisposable>(_subscriptions.Except(new[] { sub })));
                sub?.Dispose();
            });
        }

        public IDisposable SubscribeToStream(StreamKey streamKey, IHandleAsync<StreamItem> handler) {
            var sub = _bus.Subscribe(new AdHocHandler<RecordedEvent>(async (@event) => {
                if (streamKey == @event.StreamId) {
                    await handler.HandleAsync(@event);
                }
            }));
            _subscriptions.Add(sub);
            return new Disposer(() => {
                Interlocked.Exchange(
                    ref _subscriptions,
                    new ConcurrentBag<IDisposable>(_subscriptions.Except(new[] { sub })));
                sub?.Dispose();
            });
        }

        public IDisposable SubscribeToStream(StreamId streamId, IHandleAsync<StreamItem> handler) {
            var sub = _bus.Subscribe(new AdHocHandler<RecordedEvent>(async (@event) => {
                if (streamId == @event.StreamId) {
                    await handler.HandleAsync(@event);
                }
            }));
            _subscriptions.Add(sub);
            return new Disposer(() => {
                Interlocked.Exchange(
                    ref _subscriptions,
                    new ConcurrentBag<IDisposable>(_subscriptions.Except(new[] { sub })));
                sub?.Dispose();
            });
        }

        public void Dispose() {
            _cts?.Dispose();
        }
    }
}
