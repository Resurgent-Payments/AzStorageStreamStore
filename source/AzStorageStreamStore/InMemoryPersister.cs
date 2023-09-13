namespace AzStorageStreamStore;

using System.Linq;
using System.Threading.Channels;

public class InMemoryPersister : IPersister {
    private long _allStreamPosition = -1;

    private readonly CancellationTokenSource _cts = new();

    private readonly SinglyLinkedList<StreamItem> _allStream = new();

    private readonly Channel<RecordedEvent> _allStreamChannel;
    private readonly Channel<PossibleWalEntry> _streamWriterChannel;

    public ChannelReader<RecordedEvent> AllStream { get; }

    public InMemoryPersister() {
        _allStreamChannel = Channel.CreateUnbounded<RecordedEvent>(new UnboundedChannelOptions {
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

    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id)
        => ReadStreamAsync(id, 0);

    public async IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id, long revision) {
        foreach (var e in _allStream.OfType<RecordedEvent>().Where(s => s.StreamId == id).Skip((int)revision)) {
            yield return e;
        }
    }

    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key)
        => ReadStreamAsync(key, 0);

    public async IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key, long revision) {
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
                switch (expected) {
                    case -3: // no stream
                        if (!await ReadStreamAsync(StreamKey.All).AllAsync(e => e.StreamId != streamId)) {
                            onceCompleted.SetResult(WriteResult.Failed(_allStreamPosition, -1, new StreamExistsException()));
                            continue;
                        }
                        break;
                    case -2: // any stream
                        break;
                    case -1: // empty stream
                        if (_allStream.OfType<StreamCreated>().All(s => s.StreamId != streamId)) {
                            var revision = _allStream.OfType<RecordedEvent>().Max(e => e.Revision);
                            throw new WrongExpectedVersionException(ExpectedVersion.EmptyStream, revision);
                        }
                        break;
                    default:
                        var filtered = _allStream.OfType<RecordedEvent>().Where(e => e.StreamId == streamId);

                        if (!filtered.Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(_allStreamPosition, -1, new WrongExpectedVersionException(expected, ExpectedVersion.NoStream)));
                        }

                        if (filtered.Count() != expected) {
                            // if all events are appended, considered as a double request and post-back ok.
                            if (events.All(e => filtered.All(i => i.EventId != e.EventId))) {

                                onceCompleted.SetResult(WriteResult.Ok(_allStreamPosition, filtered.Max(x => x.Revision)));
                                continue;
                            }

                            // if all events were not appended
                            // -- or --
                            // only some were appended, then throw a wrong expected version.
                            if (events.Select(e => filtered.All(s => s.EventId != e.EventId)).Any()) {
                                onceCompleted.SetResult(WriteResult.Failed(_allStreamPosition,
                                    filtered.Max(x => x.Revision),
                                    new WrongExpectedVersionException(expected, filtered.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                                continue;
                            }
                        }
                        break;
                }

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
                    _allStreamPosition += 1;
                    newVersion = recorded.Revision;
                }

                onceCompleted.SetResult(WriteResult.Ok(_allStreamPosition, newVersion));
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