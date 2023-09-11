namespace AzStorageStreamStore;

using System.Linq;
using System.Threading.Channels;

public class InMemoryPersister : IDisposable {
    private long _position = -1;

    private readonly CancellationTokenSource _cts = new();

    private readonly SinglyLinkedList<RecordedEvent> _allStream = new();
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

    public async ValueTask<WriteResult> WriteAsync(StreamId id, ExpectedVersion version, EventData[] events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _streamWriterChannel.Writer.WriteAsync(new PossibleWalEntry(tcs, id, version, events));
        return await tcs.Task;
    }

    public IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id)
        => ReadAsync(id, 0);

    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id, long position) {
        if (!_streamIndex.TryGetValue(id, out var index)) {
            throw new StreamDoesNotExistException();
        }
        foreach (var e in index.Skip((int)position)) {
            yield return e.Event;
        }
    }


    public IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key)
        => ReadAsync(key, 0);
    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key, long position) {
        var subStream = _allStream
            .Where(@event => @event.Key == key)
            .Skip((int)position);

        foreach (var e in subStream) {
            if (e.Key == key) yield return e;
        }
    }

    public IAsyncEnumerable<RecordedEvent> ReadAllAsync()
        => ReadAllAsync(0);
    public async IAsyncEnumerable<RecordedEvent> ReadAllAsync(long fromPosition) {
        foreach (var @event in _allStream.Skip((int)fromPosition)) {
            yield return @event;
        }
    }

    private async Task WriteEventsImplAsync() {
        await foreach (var posssibleWalEntry in _streamWriterChannel.Reader.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expectedVersion = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            switch (expectedVersion) {
                case -3: // no stream
                    if (!_allStream.All(e => e.Key != streamId)) {
                        onceCompleted.SetResult(WriteResult.Failed(_position, -1, new StreamExistsException()));
                        continue;
                    }
                    break;
                case -2: // any stream
                    break;
                case -1: // empty stream
                    if (!_streamIndex.TryGetValue(streamId, out var emptyStreamIndex)) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, ExpectedVersion.NoStream, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, ExpectedVersion.NoStream)));
                        continue;
                    }

                    if (emptyStreamIndex.Count != 0) {
                        onceCompleted.SetResult(WriteResult.Failed(_position, ExpectedVersion.EmptyStream, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, emptyStreamIndex.Count)));
                        continue;
                    }

                    break;
                default:
                    if (!_streamIndex.TryGetValue(streamId, out var index)) {
                        onceCompleted.SetResult(WriteResult.Failed(_position, expectedVersion, new WrongExpectedVersionException(expectedVersion, ExpectedVersion.NoStream)));
                    }

                    if (index.Count != expectedVersion) {
                        // if all events are appended, considered as a double request and post-back ok.
                        if (events.All(e => index.All(i => i.Event.EventId != e.EventId))) {
                            onceCompleted.SetResult(WriteResult.Ok(_position, index.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream));
                            continue;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => index.All(s => s.Event.EventId != e.EventId)).Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(_position, index.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream,
                                new WrongExpectedVersionException(expectedVersion, index.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                            continue;
                        }
                    }
                    break;
            }

            // simulate writing to disk.

            var eventsToBeRecorded = events.Select(e => new RecordedEvent(streamId, e.EventId, _position++, e.Data))
                .ToArray();

            if (!_streamIndex.TryGetValue(streamId, out var linkTos)) {
                linkTos = new();
                _streamIndex.Add(streamId, linkTos);
            }

            foreach (var eventToBeRecorded in eventsToBeRecorded) {
                _allStream.Append(eventToBeRecorded);
                linkTos.Add(new LinkTo(linkTos.Count, eventToBeRecorded));

                // publish the recorded event.
                await _allStreamChannel.Writer.WriteAsync(eventToBeRecorded);
            }
            onceCompleted.SetResult(WriteResult.Ok(_position, linkTos.LastOrDefault()?.Revision ?? -1));
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

    public record PossibleWalEntry(TaskCompletionSource<WriteResult> OnceCompleted, StreamId Id, ExpectedVersion Version, EventData[] Events);
}