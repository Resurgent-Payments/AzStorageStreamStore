namespace LvStreamStore;

using LvStreamStore.Messaging;
using LvStreamStore.Subscriptions;

using Microsoft.Extensions.Logging;

public class EmbeddedEventStreamClient : IEventStreamClient {
    private readonly CancellationTokenSource _cts = new();
    private readonly EventStream _eventStream;
    private readonly AsyncDispatcher _dispatcher;
    private readonly ILoggerFactory _loggerFactory;

    private EventStreamPoller _watcher;
    private bool _isConnected = false;

    public EmbeddedEventStreamClient(AsyncDispatcher dispatcher, EventStream eventStream, ILoggerFactory loggerFactory) {
        _dispatcher = dispatcher;
        _eventStream = eventStream;
        _loggerFactory = loggerFactory;
        _watcher = null!;
    }

    public Task Connect() {
        _watcher ??= new EventStreamPoller(_dispatcher, _eventStream, new(), _loggerFactory);
        _watcher.StartPolling();
        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task Disconnect() {
        _isConnected = false;
        _watcher?.Dispose();
        _watcher = null!;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key) {
        if (!_isConnected) { throw new EventStreamDisconnectedException(); }
        return _eventStream.ReadAsync(key);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id) {
        if (!_isConnected) { throw new EventStreamDisconnectedException(); }
        return _eventStream.ReadAsync(id);
    }


    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(IReceiver<StreamMessage> handler) {
        if (!_isConnected) { throw new EventStreamDisconnectedException(); }
        return Task.FromResult(_watcher.SubscribeToStream(handler));
    }

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamKey streamKey, IReceiver<StreamMessage> handler) {
        if (!_isConnected) { throw new EventStreamDisconnectedException(); }
        return Task.FromResult(_watcher.SubscribeToStream(streamKey, handler));
    }

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, IReceiver<StreamMessage> handler) {
        if (!_isConnected) { throw new EventStreamDisconnectedException(); }
        return Task.FromResult(_watcher.SubscribeToStream(streamId, handler));
    }

    /// <inheritdoc />
    public ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events) {
        if (!_isConnected) { throw new EventStreamDisconnectedException(); }
        return _eventStream.AppendAsync(key, version, events);
    }

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
