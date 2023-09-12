namespace AzStorageStreamStore;

using System.Collections.Concurrent;

public class LocalStoreClient : IStoreClient {
    private readonly CancellationTokenSource _cts = new();

    private readonly InMemoryPersister _inMemoryPersister = new();

    private ConcurrentBag<Action<RecordedEvent>> _subscriptions = new();
    private readonly Dictionary<StreamKey, ConcurrentBag<Action<RecordedEvent>>> _streamKeySubscriptions = new();
    private readonly Dictionary<StreamId, ConcurrentBag<Action<RecordedEvent>>> _streamIdSubscriptions = new();

    public Task InitializeAsync() {
        // holds the task that manages pumping events from the _publisher stream to all handlers.
        Task.Factory.StartNew(MessagePump, _cts.Token);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events)
        => _inMemoryPersister.WriteAsync(key, version, events);

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key)
        => _inMemoryPersister.ReadAsync(key);

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id)
        => _inMemoryPersister.ReadAsync(id);

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToAllAsync(Action<RecordedEvent> handler) {
        _subscriptions.Add(handler);
        return Task.FromResult<IDisposable>(
            new StreamDisposer(() =>
            Interlocked.Exchange(
                ref _subscriptions,
                new ConcurrentBag<Action<RecordedEvent>>(_subscriptions.Except(new[] { handler }))))
        );
    }

    /// <inheritdoc />
    public async Task<IDisposable> SubscribeToAllFromAsync(long position, Action<RecordedEvent> handler) {
        await foreach (var @event in _inMemoryPersister.ReadAllAsync(position)) {
            handler.Invoke(@event);
        }
        return new StreamDisposer(() =>
            Interlocked.Exchange(
                ref _subscriptions,
                new ConcurrentBag<Action<RecordedEvent>>(_subscriptions.Except(new[] { handler }))));
    }

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamKey key, Action<RecordedEvent> handler)
        => SubscribeToStreamFromAsync(key, 0, handler);

    /// <inheritdoc />
    public async Task<IDisposable> SubscribeToStreamFromAsync(StreamKey key, long revision, Action<RecordedEvent> handler) {
        // build subscription for specific stream.
        if (!_streamKeySubscriptions.TryGetValue(key, out var bag)) {
            bag = new();
            _streamKeySubscriptions.Add(key, bag);
        }

        await foreach (var @event in _inMemoryPersister.ReadAsync(key, revision)) {
            foreach (var slice in key) {
                handler.Invoke(@event);
            }
        }

        bag.Add(handler);

        return new StreamDisposer(() => {
            Interlocked.Exchange(
                ref bag,
                new ConcurrentBag<Action<RecordedEvent>>(bag.Except(new[] { handler }))
            );
            _streamKeySubscriptions[key] = bag;
        });
    }

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, Action<RecordedEvent> handler)
        => SubscribeToStreamFromAsync(streamId, 0, handler);

    /// <inheritdoc />
    public async Task<IDisposable> SubscribeToStreamFromAsync(StreamId streamId, long revision, Action<RecordedEvent> handler) {
        if (!_streamIdSubscriptions.TryGetValue(streamId, out var bag)) {
            bag = new();
            _streamIdSubscriptions.Add(streamId, bag);
        }

        await foreach (var @event in _inMemoryPersister.ReadAsync(streamId, revision)) {
            handler.Invoke(@event);
        }

        bag.Add(handler);

        return new StreamDisposer(() => {
            Interlocked.Exchange(
                ref bag,
                new ConcurrentBag<Action<RecordedEvent>>(bag.Except(new[] { handler }))
            );
            _streamIdSubscriptions[streamId] = bag;
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

                var keys = (StreamKey)e.StreamId;

                foreach (var key in keys) {
                    if (!_streamKeySubscriptions.TryGetValue(key, out var keyBag)) continue;
                    foreach (var act in keyBag) {
                        act.Invoke(e);
                    }
                }

                if (!_streamIdSubscriptions.TryGetValue(e.StreamId, out var idBag)) continue;
                foreach (var id in idBag) {
                    id.Invoke(e);
                }
            }
        }
    }
}
