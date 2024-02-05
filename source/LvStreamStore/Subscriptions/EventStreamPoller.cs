namespace LvStreamStore.Subscriptions;

using System;
using System.Collections.Concurrent;

internal class EventStreamPoller : IDisposable {
    private readonly CancellationTokenSource _cts = new();
    private readonly Messaging.AsyncDispatcher _dispatcher;
    private readonly EventStreamPollerOptions _options;
    private ConcurrentBag<IDisposable> _subscriptions = [];
    EventStreamReader _streamReader;

    public EventStreamPoller(Messaging.AsyncDispatcher dispatcher, EventStream stream, EventStreamPollerOptions options) {
        _dispatcher = dispatcher;
        _streamReader = stream.GetReader();
        _options = options;

        StartPolling();
    }

    public IDisposable SubscribeToStream(Messaging.IReceiver<StreamItem> handler) {
        var msg = Messaging.AsyncDispatcher.Register(handler);
        AsyncHelper.RunSync(() => _dispatcher.HandleAsync(msg));
        if (msg is IDisposable disposable) {
            _subscriptions.Add(disposable);
            return new Disposer(() => {
                disposable.Dispose();
                Interlocked.Exchange(
                    ref _subscriptions,
                    new ConcurrentBag<IDisposable>(_subscriptions.Except(new[] { disposable })));
            });
        }
        return new Disposer(() => { });
    }

    public IDisposable SubscribeToStream(StreamKey streamKey, Messaging.IReceiver<StreamItem> handler) {
        var filter = new Messaging.FilteredReceiver<StreamItem>(handler,
            msg => msg is RecordedEvent @event && streamKey == @event.StreamId);
        var msg = Messaging.AsyncDispatcher.Register(filter);
        AsyncHelper.RunSync(() => _dispatcher.HandleAsync(msg));
        if (msg is IDisposable disposable) {
            _subscriptions.Add(disposable);
            return new Disposer(() => {
                disposable.Dispose();
                Interlocked.Exchange(
                    ref _subscriptions,
                    new ConcurrentBag<IDisposable>(_subscriptions.Except(new[] { disposable })));
            });
        }
        return new Disposer(() => { });
    }

    public IDisposable SubscribeToStream(StreamId streamId, Messaging.IReceiver<StreamItem> handler) {
        var filter = new Messaging.FilteredReceiver<StreamItem>(handler,
            msg => msg is RecordedEvent @event && streamId == @event.StreamId);
        var msg = Messaging.AsyncDispatcher.Register(filter);
        AsyncHelper.RunSync(() => _dispatcher.HandleAsync(msg));
        if (msg is IDisposable disposable) {
            _subscriptions.Add(disposable);
            return new Disposer(() => {
                disposable.Dispose();
                Interlocked.Exchange(
                    ref _subscriptions,
                    new ConcurrentBag<IDisposable>(_subscriptions.Except(new[] { disposable })));
            });
        }
        return new Disposer(() => { });
    }

    private async void StartPolling() {
        await Task.Yield();

        while (!_cts.IsCancellationRequested) {
            await foreach (var msg in _streamReader) {
                await _dispatcher.BroadcastAsync(msg);
            }

            await Task.Delay(_options.PollingInterval).ConfigureAwait(false);
        }
    }

    public void Dispose() {
        foreach (var d in _subscriptions) {
            d?.Dispose();
        }
        _cts?.Dispose();
    }
}
