namespace LvStreamStore;

using LvStreamStore.Subscriptions;

using Microsoft.Extensions.Logging;

public class EmbeddedEventStreamClient : IEventStreamClient {
    private readonly CancellationTokenSource _cts = new();

    private readonly EventStream _eventStream;
    private readonly EventStreamPoller _watcher;

    public EmbeddedEventStreamClient(EventStream eventStream, ILoggerFactory loggerFactory) {
        _eventStream = eventStream;
        _watcher = new EventStreamPoller(loggerFactory, _eventStream, new());
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
    public Task<IDisposable> SubscribeToStreamAsync(Func<StreamItem, ValueTask> handler)
        => Task.FromResult(_watcher.SubscribeToStream(new AdHocHandler<StreamItem>(handler)));

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamKey streamKey, Func<RecordedEvent, ValueTask> handler)
        => Task.FromResult(_watcher.SubscribeToStream(streamKey, new AdHocHandler<StreamItem>(async item => {
            if (item is RecordedEvent @event && streamKey == @event.StreamId) {
                await handler.Invoke(@event);
            }
        })));

    /// <inheritdoc />
    public Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, Func<RecordedEvent, ValueTask> handler)
        => Task.FromResult(_watcher.SubscribeToStream(streamId, new AdHocHandler<StreamItem>(async item => {
            if (item is RecordedEvent @event && streamId == @event.StreamId) {
                await handler.Invoke(@event);
            }
        })));

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
