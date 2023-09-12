namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public abstract class DurablePersisterBase : IPersister {
    protected CancellationTokenSource TokenSource { get; } = new();

    private Channel<PossibleWalEntry> _walWriter;
    private Channel<RecordedEvent> _storedEvents;

    protected ChannelReader<PossibleWalEntry> WalWriter => _walWriter.Reader;

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
                if (!await ReadAsync(AzStorageStreamStore.AllStream.SingleTenant).AllAsync(e => e.StreamId != id)) {
                    onceCompleted.SetResult(WriteResult.Failed(-1, -1, new StreamExistsException()));
                    return false;
                }
                break;
            case -2: // any stream
            case -1: // empty stream
                return true; // how do i know exactly if i have an empty stream?
            default:
                long eventsInStream = 0;
                var streamEvents = await ReadAsync(AzStorageStreamStore.AllStream.SingleTenant).Where(@event => @event.StreamId == id).ToListAsync();

                if (!streamEvents.Any()) {
                    onceCompleted.SetResult(WriteResult.Failed(-1, -1, new WrongExpectedVersionException(expectedVersion, ExpectedVersion.NoStream)));
                }

                if (streamEvents.Count != expectedVersion) {
                    // if all events are appended, considered as a double request and post-back ok.
                    if (!streamEvents.All(e => events.All(i => e.EventId != i.EventId))) {
                        onceCompleted.SetResult(WriteResult.Ok(-1, streamEvents.Max(x => x.Revision)));
                        return true;
                    }

                    // if all events were not appended
                    // -- or --
                    // only some were appended, then throw a wrong expected version.
                    if (events.Select(e => streamEvents.All(s => s.EventId != e.EventId)).Any()) {
                        onceCompleted.SetResult(WriteResult.Failed(-1,
                            -1,
                            new WrongExpectedVersionException(expectedVersion, streamEvents.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                        return false;
                    }
                }
                break;
        }
        return true;
    }
}