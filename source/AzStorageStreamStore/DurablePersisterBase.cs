namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public abstract class DurablePersisterBase : IPersister {
    private string _fileName = "chunk.dat";
    protected CancellationTokenSource TokenSource { get; } = new();

    private Channel<PossibleWalEntry> _walWriter;
    private Channel<RecordedEvent> _storedEvents;

    protected ChannelReader<PossibleWalEntry> WalWriter => _walWriter.Reader;


    protected virtual long Position { get; }
    protected LocalDiskDurablePersisterOptions Options { get; }


    public ChannelReader<RecordedEvent> AllStream => _storedEvents.Reader;

    public DurablePersisterBase(IOptions<LocalDiskDurablePersisterOptions> options) {
        Options = options.Value;
        
        _storedEvents = Channel.CreateUnbounded<RecordedEvent>(new UnboundedChannelOptions {
            SingleWriter = true,
            SingleReader = true,
            AllowSynchronousContinuations = false
        });
        TokenSource.Token.Register(() => _storedEvents.Writer.Complete());

        _walWriter = Channel.CreateUnbounded<PossibleWalEntry>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        TokenSource.Token.Register(() => _walWriter.Writer.Complete());
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing) { }

    protected string CalculateFileName(StreamId streamId)
        => Path.Combine(Options.BaseDataPath, streamId.TenantId, _fileName);

    public virtual IAsyncEnumerable<RecordedEvent> ReadAllAsync()
        => ReadAllAsync(0);

    public abstract IAsyncEnumerable<RecordedEvent> ReadAllAsync(long fromPosition);

    public virtual IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id)
        => ReadAsync(id, 0);

    public abstract IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id, long position);

    public IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key)
        => ReadAsync(key, 0);

    public abstract IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key, long position);

    public async ValueTask<WriteResult> WriteAsync(StreamId id, ExpectedVersion version, EventData[] events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _walWriter.Writer.WriteAsync(new PossibleWalEntry(tcs, id, version, events));
        return await tcs.Task;
    }

    protected virtual async Task<bool> ValidateValidWrite(TaskCompletionSource<WriteResult> onceCompleted, StreamId id, long expectedVersion, EventData[] events) {
        switch (expectedVersion) {
            case -3: // no stream
                if (!await ReadAllAsync().AllAsync(e => e.StreamId != id)) {
                    onceCompleted.SetResult(WriteResult.Failed(Position, -1, new StreamExistsException()));
                    return false;
                }
                break;
            case -2: // any stream
            case -1: // empty stream
                return true; // how do i know exactly if i have an empty stream?
            default:
                long eventsInStream = 0;
                var streamEvents = await ReadAllAsync().Where(@event => @event.StreamId == id).ToListAsync();

                if (!streamEvents.Any()) {
                    onceCompleted.SetResult(WriteResult.Failed(Position, expectedVersion, new WrongExpectedVersionException(expectedVersion, ExpectedVersion.NoStream)));
                }

                if (streamEvents.Count != expectedVersion) {
                    // if all events are appended, considered as a double request and post-back ok.
                    if (!streamEvents.All(e => events.All(i => e.EventId != i.EventId))) {
                        onceCompleted.SetResult(WriteResult.Ok(Position, streamEvents.Count));
                        return true;
                    }

                    // if all events were not appended
                    // -- or --
                    // only some were appended, then throw a wrong expected version.
                    if (events.Select(e => streamEvents.All(s => s.EventId != e.EventId)).Any()) {
                        onceCompleted.SetResult(WriteResult.Failed(Position, streamEvents.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream,
                            new WrongExpectedVersionException(expectedVersion, streamEvents.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                        return false;
                    }
                }
                break;
        }
        return true;
    }
}