namespace LvStreamStore;
public class EmbeddedEventStreamClient : IEventStreamClient {
    private readonly CancellationTokenSource _cts = new();

    private readonly EventStream _eventStream;

    public EmbeddedEventStreamClient(EventStream eventStream) {
        _eventStream = eventStream;
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
    public Task<IDisposable> SubscribeToStreamAsync(Func<RecordedEvent, Task> handler)
        => _eventStream.SubscribeToStreamAsync(handler);

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamKey streamKey, Func<RecordedEvent, Task> handler)
        => _eventStream.SubscribeToStreamAsync(streamKey, handler.Invoke);
 
    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, Func<RecordedEvent, Task> handler)
        => _eventStream.SubscribeToStreamAsync(streamId, handler.Invoke);

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

        _eventStream.Dispose();
        _cts.Cancel();
        _cts.Dispose();

        _disposed = true;
    }
}
