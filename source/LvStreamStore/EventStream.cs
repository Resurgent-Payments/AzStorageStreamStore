namespace LvStreamStore;

using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Options;

public abstract class EventStream : IDisposable {
    protected static class Constants {
        public static byte NULL = 0x00;
        public static byte EndOfRecord = 0x1E;
    }

    private readonly IEventStreamOptions _options;
    private readonly Channel<StreamItem> _stream;
    private readonly Channel<WriteToStreamArgs> _streamWriter;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    protected ChannelReader<WriteToStreamArgs> StreamWriter => _streamWriter.Reader;
    protected ChannelWriter<StreamItem> StreamPublisher => _stream.Writer;

    public int Checkpoint { get; protected set; }

    public EventStream(IOptions<IEventStreamOptions> options) {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _stream = Channel.CreateUnbounded<StreamItem>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });
        _streamWriter = Channel.CreateUnbounded<WriteToStreamArgs>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _cts.Token.Register(() => _streamWriter.Writer.Complete());
        Task.Factory.StartNew(StreamWriterImpl, _cts.Token);
    }

    public IAsyncEnumerable<StreamItem> ListenForChangesAsync(CancellationToken token = default) => _stream.Reader.ReadAllAsync(token);

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamId streamId)
        => ReadStreamFromAsync(streamId, 0);

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamKey streamKey)
        => ReadStreamFromAsync(streamKey, 0);

    public async IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamId streamId, int revision) {
        // full scan.
        if (await ReadLogAsync().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) throw new StreamDoesNotExistException();

        if (revision == int.MaxValue) yield break;

        // second full scan
        await foreach (var e in ReadLogAsync().OfType<RecordedEvent>().Where(s => s.StreamId == streamId).Skip(revision)) {
            yield return e;
        }
    }

    public async IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamKey streamKey, int revision) {
        if (revision == int.MaxValue) yield break;

        // full scan
        await foreach (var e in ReadLogAsync().OfType<RecordedEvent>().Where(s => s.StreamId == streamKey).Skip(revision)) {
            yield return e;
        }
    }

    public async ValueTask<WriteResult> AppendToStreamAsync(StreamId streamId, ExpectedVersion version, IEnumerable<EventData> events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _streamWriter.Writer.WriteAsync(new WriteToStreamArgs(tcs, streamId, version, events));
        return await tcs.Task;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposing || _disposed) return;
        _cts.Cancel();
        _disposed = true;
    }

    protected async Task StreamWriterImpl() {
        await foreach (var posssibleWalEntry in StreamWriter.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expected = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            try {
                if (!await PassesStreamValidationAsync(onceCompleted, streamId, expected, events)) continue;


                // refactor to create one memorystream to be appended to a WAL.

                // first full scan
                // if we don't have a StreamCreated event, we need to append one now.
                if (await ReadLogAsync().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) {
                    // write the stream created event.
                    var ms = new MemoryStream();
                    var created = new StreamCreated(streamId);

                    JsonSerializer.Serialize(ms, created, _options.JsonOptions);
                    ms.WriteByte(Constants.EndOfRecord);
                    await Persist(ms.ToArray());
                }

                // second full scan
                var revision = (await ReadLogAsync()
                    .OfType<RecordedEvent>()
                    .Where(e => e.StreamId == streamId)
                    .LastOrDefaultAsync())?.Revision ?? -1L;

                // cache the current checkpoint.

                foreach (var @event in events) {
                    revision += 1;
                    var recorded = new RecordedEvent(streamId, @event.EventId, revision, @event.Type, @event.Metadata, @event.Data);

                    // write the stream created event.
                    var ms = new MemoryStream();

                    var startIdx = Checkpoint;
                    JsonSerializer.Serialize(ms, recorded, _options.JsonOptions);
                    ms.WriteByte(Constants.EndOfRecord);
                    await Persist(ms.ToArray());
                    var eventOffset = Checkpoint - startIdx;
                    // todo: write startIdx and eventOffset to 'index'

                    // publish the recorded event.
                    await StreamPublisher.WriteAsync(recorded);
                }

                // capture the offset here.

                // write the index log entry.


                // dump WAL to disc
                // - - - - - 
                // emit message to append WAL to data file.
                //  - capture last-modified for data file, send to wal writer
                //  - if wal writer finds last-modified changed, then reject write request.

                onceCompleted.SetResult(WriteResult.Ok(revision));
            }
            catch (Exception exc) {
                onceCompleted.SetResult(WriteResult.Failed(-1, exc));
            }
        }
    }

    protected virtual async ValueTask<bool> PassesStreamValidationAsync(TaskCompletionSource<WriteResult> onceCompleted, StreamId streamId, ExpectedVersion expected, IEnumerable<EventData> events) {

        try {
            switch (expected) {
                case -3: // no stream
                    if (!await ReadStreamAsync(StreamKey.All).AllAsync(e => e.StreamId != streamId)) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, new StreamExistsException()));
                        return false;
                    }
                    break;
                case -2: // any stream
                    break;
                case -1: // empty stream
                    if (await ReadStreamAsync(StreamKey.All).AllAsync(s => s.StreamId != streamId)) {
                        var revision = ReadStreamAsync(StreamKey.All).OfType<RecordedEvent>().MaxAsync(e => e.Revision);
                        onceCompleted.SetResult(WriteResult.Failed(-1, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, -1)));
                        return false;
                    } else {
                        // check for duplicates here.
                        var nonEmptyStreamEvents = await ReadStreamAsync(StreamKey.All).OfType<RecordedEvent>().Where(s => s.StreamId == streamId).ToListAsync();

                        if (nonEmptyStreamEvents.Any()) {
                            // if all events are appended, considered as a double request and post-back ok.
                            if (!nonEmptyStreamEvents.All(e => events.All(i => e.EventId != i.EventId))) {
                                onceCompleted.SetResult(WriteResult.Ok(nonEmptyStreamEvents.Max(x => x.Revision)));
                                return false;
                            }
                        }
                    }
                    break;
                default:
                    var filtered = await ReadStreamAsync(StreamKey.All).OfType<RecordedEvent>().Where(e => e.StreamId == streamId).ToListAsync();

                    if (!filtered.Any()) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, new WrongExpectedVersionException(expected, ExpectedVersion.NoStream)));
                        return false;
                    }

                    if (filtered.Count() != expected) {
                        // if all events are appended, considered as a double request and post-back ok.
                        if (events.All(e => filtered.All(i => i.EventId != e.EventId))) {

                            onceCompleted.SetResult(WriteResult.Ok(filtered.Max(x => x.Revision)));
                            return false;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => filtered.All(s => s.EventId != e.EventId)).Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(filtered.Max(x => x.Revision),
                                new WrongExpectedVersionException(expected, filtered.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                            return false;
                        }
                    }
                    break;
            }
        }
        catch (Exception exc) {
            return false;
        }

        return true;
    }

    protected abstract IAsyncEnumerable<StreamItem> ReadLogAsync();

    protected abstract Task Persist(byte[] data);
}