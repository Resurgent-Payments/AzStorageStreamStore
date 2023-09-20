namespace AzStorageStreamStore;

using System.Linq;
using System.Threading.Channels;

public class SingleTenantInMemoryPersister : IPersister {
    internal long Position { get; private set; } = -1;
    long IPersister.Position => Position;

    private readonly PersistenceUtils _utils;

    private readonly CancellationTokenSource _cts = new();

    private readonly SinglyLinkedList<StreamItem> _allStream = new();

    private readonly Channel<StreamItem> _allStreamChannel;
    private readonly Channel<PossibleWalEntry> _streamWriterChannel;

    public ChannelReader<StreamItem> AllStream { get; }

    public SingleTenantInMemoryPersister() {
        _utils = new(this);
        _allStreamChannel = Channel.CreateUnbounded<StreamItem>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
        AllStream = _allStreamChannel.Reader;
        _cts.Token.Register(() => _allStreamChannel.Writer.Complete());

        _streamWriterChannel = Channel.CreateUnbounded<PossibleWalEntry>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _cts.Token.Register(() => _streamWriterChannel.Writer.Complete());
        Task.Factory.StartNew(WriteEventsImplAsync, _cts.Token);
    }

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamId id)
        => ReadStreamAsync(id, 0);

    public async IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamId id, long revision) {
        if (_allStream.OfType<StreamCreated>().All(sc => sc.StreamId != id)) throw new StreamDoesNotExistException();

        var foundEvents = _allStream.OfType<RecordedEvent>().Where(s => s.StreamId == id).ToArray();

        foreach (var e in foundEvents.Skip((int)revision)) {
            yield return e;
        }
    }

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamKey key)
        => ReadStreamAsync(key, 0);

    public async IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamKey key, long revision) {
        var subStream = _allStream
            .OfType<RecordedEvent>()
            .Where(@event => @event.StreamId == key)
            .Skip((int)revision);

        foreach (var e in subStream) {
            if (e.StreamId == key) yield return e;
        }
    }

    public async ValueTask<WriteResult> AppendToStreamAsync(StreamId id, ExpectedVersion version, EventData[] events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _streamWriterChannel.Writer.WriteAsync(new PossibleWalEntry(tcs, id, version, events));
        return await tcs.Task;
    }

    public ValueTask Truncate() {
        _allStream.Clear();
        return ValueTask.CompletedTask;
    }

    private async Task WriteEventsImplAsync() {
        await foreach (var posssibleWalEntry in _streamWriterChannel.Reader.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expected = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            try {
                if (!await _utils.PassesStreamValidationAsync(onceCompleted, streamId, expected, events)) continue;

                // if we don't have a StreamCreated event, we need to append one now.
                if (_allStream.OfType<StreamCreated>().All(e => e.StreamId != streamId)) {
                    _allStream.Append(new StreamCreated(streamId));
                }

                var newVersion = -1L;
                foreach (var @event in events) {
                    var filtered = _allStream.OfType<RecordedEvent>()
                        .Where(e => e.StreamId == streamId)
                        .ToArray();
                    var nextRevision = filtered.Any()
                        ? filtered.Max(f => f.Revision) + 1
                        : 0;
                    var recorded = new RecordedEvent(streamId, @event.EventId, nextRevision, @event.Data);

                    _allStream.Append(recorded);

                    // publish the recorded event.
                    await _allStreamChannel.Writer.WriteAsync(recorded);
                    Position += 1;
                    newVersion = recorded.Revision;
                }

                onceCompleted.SetResult(WriteResult.Ok(Position, newVersion));
            }
            catch (Exception exc) {
                onceCompleted.SetResult(WriteResult.Failed(-1, -1, exc));
            }
        }
    }

    private bool _disposed = false;

    public void Dispose() => Dispose(true);

    protected virtual void Dispose(bool disposing) {
        if (_disposed || !disposing) return;

        _cts.Cancel();
        _cts.Dispose();

        _disposed = true;

        GC.SuppressFinalize(this);
    }
}