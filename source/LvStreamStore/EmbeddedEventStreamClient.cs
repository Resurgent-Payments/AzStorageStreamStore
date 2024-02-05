namespace LvStreamStore;

using LvStreamStore.Messaging;
using LvStreamStore.Subscriptions;

public class EmbeddedEventStreamClient : IEventStreamClient {
    private readonly CancellationTokenSource _cts = new();
    private readonly EventStream _eventStream;
    private readonly EventStreamPoller _watcher;
    private readonly AsyncDispatcher _dispatcher;

    public EmbeddedEventStreamClient(AsyncDispatcher dispatcher, EventStream eventStream) {
        _dispatcher = dispatcher;
        _eventStream = eventStream;
        _watcher = new EventStreamPoller(_dispatcher, _eventStream, new());
    }

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key)
        => _eventStream.ReadAsync(key);

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id)
        => _eventStream.ReadAsync(id);


    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(IReceiver<StreamMessage> handler)
        => Task.FromResult(_watcher.SubscribeToStream(handler));

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamKey streamKey, IReceiver<StreamMessage> handler)
        => Task.FromResult(_watcher.SubscribeToStream(streamKey, handler));

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, IReceiver<StreamMessage> handler)
        => Task.FromResult(_watcher.SubscribeToStream(streamId, handler));

    /// <inheritdoc />
    public ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events)
        => _eventStream.AppendAsync(key, version, events);

    private bool _disposed = false;
    public void Dispose() {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed || !disposing) return;

        _watcher?.Dispose();
        _eventStream?.Dispose();
        _cts.Cancel();
        _cts.Dispose();

        _disposed = true;
    }
}
