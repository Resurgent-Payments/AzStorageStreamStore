namespace AzStorageStreamStore;

using System.Collections.Concurrent;

public class LocalStoreClient : IStoreClient {
    private readonly CancellationTokenSource _cts = new();

    private readonly InMemoryPersister _inMemoryPersister = new();

    private ConcurrentBag<Action<RecordedEvent>> _subscriptions = new();
    private readonly Dictionary<StreamKey, ConcurrentBag<Action<RecordedEvent>>> _streamSubscriptions = new();

    public Task InitializeAsync() {
        // holds the task that manages pumping events from the _publisher stream to all handlers.
        Task.Factory.StartNew(MessagePump, _cts.Token);

        return Task.CompletedTask;
    }

    public ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events)
        => _inMemoryPersister.WriteAsync(key, version, events);

    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key)
        => _inMemoryPersister.ReadAsync(key);

    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id)
        => _inMemoryPersister.ReadAsync(id);

    public Task<IDisposable> SubscribeToAllAsync(Action<RecordedEvent> eventHandler) {
        _subscriptions.Add(eventHandler);
        return Task.FromResult<IDisposable>(
            new StreamDisposer(() =>
            Interlocked.Exchange(
                ref _subscriptions,
                new ConcurrentBag<Action<RecordedEvent>>(_subscriptions.Except(new[] { eventHandler }))))
        );
    }

    public async Task<IDisposable> SubscribeToAllFromAsync(long position, Action<RecordedEvent> eventHandler) {
        await foreach (var @event in _inMemoryPersister.ReadAllAsync(position)) {
            eventHandler.Invoke(@event);
        }
        return new StreamDisposer(() =>
            Interlocked.Exchange(
                ref _subscriptions,
                new ConcurrentBag<Action<RecordedEvent>>(_subscriptions.Except(new[] { eventHandler }))));
    }

    public async Task<IDisposable> SubscribeToStreamAsync(StreamKey key, Action<RecordedEvent> eventHandler) {
        // build subscription for specific stream.
        if (!_streamSubscriptions.TryGetValue(key, out var bag)) {
            bag = new();
            _streamSubscriptions.TryAdd(key, bag);
        }

        await foreach (var @event in _inMemoryPersister.ReadAsync(key)) {
            eventHandler.Invoke(@event);
        }

        bag.Add(eventHandler);

        return new StreamDisposer(() =>
            Interlocked.Exchange(
                ref bag,
                new ConcurrentBag<Action<RecordedEvent>>(bag.Except(new[] { eventHandler }))
            )
        );
    }

    public async Task<IDisposable> SubscribeToStreamFromAsync(long position, StreamKey key, Action<RecordedEvent> eventHandler) {
        // build subscription for specific stream.
        if (!_streamSubscriptions.TryGetValue(key, out var bag)) {
            bag = new();
            _streamSubscriptions.Add(key, bag);
        }

        await foreach (var @event in _inMemoryPersister.ReadAsync(key, position))
            foreach (var slice in key) {
                eventHandler.Invoke(@event);
            }

        bag.Add(eventHandler);

        return new StreamDisposer(() => {
            Interlocked.Exchange(
                ref bag,
                new ConcurrentBag<Action<RecordedEvent>>(bag.Except(new[] { eventHandler }))
            );
            _streamSubscriptions[key] = bag;
        });
    }


    private bool _disposed = false;
    public void Dispose() {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed || !disposing) return;

        _inMemoryPersister.Dispose();
        _cts.Cancel();
        _cts.Dispose();

        _disposed = true;
    }

    private async void MessagePump() {
        while (!_cts.IsCancellationRequested) {
            await foreach (var e in _inMemoryPersister.AllStream.ReadAllAsync(_cts.Token)) {
                if (_cts.IsCancellationRequested) { return; }
                foreach (var allAction in _subscriptions) {
                    allAction.Invoke(e);
                }

                var keys = (StreamKey)e.Key;

                foreach (var key in keys) {
                    if (!_streamSubscriptions.TryGetValue(key, out var bag)) continue;
                    foreach (var act in bag) {
                        act.Invoke(e);
                    }
                }
            }
        }
    }
}
