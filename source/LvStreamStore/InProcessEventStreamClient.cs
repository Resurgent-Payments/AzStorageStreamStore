namespace LvStreamStore;

using System.Reactive;

public class InProcessEventStreamClient : IEventStreamClient {
    private readonly CancellationTokenSource _cts = new();

    private readonly EventStream _eventStream;

    public InProcessEventStreamClient(EventStream eventStream) {
        _eventStream = eventStream;
    }

    public Task InitializeAsync() {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events)
        => _eventStream.AppendAsync(key, version, events);

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key)
        => _eventStream.ReadAsync(key).OfType<RecordedEvent>();

    /// <inheritdoc />
    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id)
        => _eventStream.ReadAsync(id).OfType<RecordedEvent>();


    /// <inheritdoc />
    public IDisposable SubscribeToAll(Action<RecordedEvent> handler) {
        return _eventStream.Subscribe(Observer.Create((StreamItem item) => {
            if (item is RecordedEvent) {
                handler.Invoke((RecordedEvent)item);
            }
        }));
    }

    /// <inheritdoc />
    public IDisposable SubscribeToStream(StreamKey key, Action<RecordedEvent> handler) {
        return _eventStream.SubscribeToStream(key, Observer.Create((StreamItem item) => {
            if (item is RecordedEvent && key == item.StreamId) {
                handler.Invoke((RecordedEvent)item);
            }
        }));
    }

    /// <inheritdoc />
    public async Task<IDisposable> SubscribeToStreamFromAsync(StreamKey key, int revision, Action<RecordedEvent> handler) {
        return await _eventStream.SubscribeToStreamFromAsync(key, revision, Observer.Create((StreamItem item) => {
            if (item is RecordedEvent && key == item.StreamId) {
                handler.Invoke((RecordedEvent)item);
            }
        }));
    }

    /// <inheritdoc />
    public IDisposable SubscribeToStream(StreamId streamId, Action<RecordedEvent> handler) {
        return _eventStream.SubscribeToStream(streamId, Observer.Create((StreamItem item) => {
            if (item is RecordedEvent && streamId == item.StreamId) {
                handler.Invoke((RecordedEvent)item);
            }
        }));
    }

    /// <inheritdoc />
    public async Task<IDisposable> SubscribeToStreamFromAsync(StreamId streamId, int revision, Action<RecordedEvent> handler) {
        return await _eventStream.SubscribeToStreamFromAsync(streamId, revision, Observer.Create((StreamItem item) => {
            if (item is RecordedEvent && streamId == item.StreamId) {
                handler.Invoke((RecordedEvent)item);
            }
        }));
    }


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
