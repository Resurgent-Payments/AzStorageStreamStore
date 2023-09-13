namespace AzStorageStreamStore;

using System.Linq;
using System.Threading.Channels;

public class InMemoryPersister : IPersister {
    private long _allStreamPosition = -1;

    private readonly CancellationTokenSource _cts = new();

    private readonly SinglyLinkedList<StreamItem> _allStream = new();
    private readonly Dictionary<StreamId, List<LinkTo>> _streamIndex = new();

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
        if (!_streamIndex.TryGetValue(id, out var index)) {
            throw new StreamDoesNotExistException();
        }
        foreach (var e in index.Skip((int)revision)) {
            yield return e.Event;
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

    private async Task WriteEventsImplAsync() {
        await foreach (var posssibleWalEntry in _streamWriterChannel.Reader.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expected = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

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
                    if (!_streamIndex.TryGetValue(streamId, out var emptyStreamIndex)) {
                        onceCompleted.SetResult(WriteResult.Failed(ExpectedVersion.EmptyStream, -1, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, ExpectedVersion.NoStream)));
                        continue;
                    }

                    if (emptyStreamIndex.Count != 0) {
                        onceCompleted.SetResult(WriteResult.Failed(ExpectedVersion.EmptyStream, -1, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, emptyStreamIndex.Count)));
                        continue;
                    }

                    break;
                default:
                    if (!_streamIndex.TryGetValue(streamId, out var index)) {
                        onceCompleted.SetResult(WriteResult.Failed(_allStreamPosition, -1, new WrongExpectedVersionException(expected, ExpectedVersion.NoStream)));
                    }

                    if (index.Count != expected) {
                        // if all events are appended, considered as a double request and post-back ok.
                        if (events.All(e => index.All(i => i.Event.EventId != e.EventId))) {

                            onceCompleted.SetResult(WriteResult.Ok(_allStreamPosition, index.Max(x => x.Revision)));
                            continue;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => index.OfType<LinkTo>().All(s => s.Event.EventId != e.EventId)).Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(_allStreamPosition,
                                index.Max(x => x.Revision),
                                new WrongExpectedVersionException(expected, index.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                            continue;
                        }
                    }
                    break;
            }

            if (!_streamIndex.TryGetValue(streamId, out var linkTos)) {
                linkTos = new();
                _streamIndex.Add(streamId, linkTos);
            }

            var newVersion = -1L;
            foreach (var @event in events) {
                var recorded = new RecordedEvent(streamId, @event.EventId, linkTos.Count, @event.Data);

                _allStream.Append(recorded);
                linkTos.Add(new LinkTo(linkTos.Count, recorded));

                // publish the recorded event.
                await _allStreamChannel.Writer.WriteAsync(recorded);
                newVersion = recorded.Revision;
            }

            onceCompleted.SetResult(WriteResult.Ok(_allStreamPosition, newVersion));
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