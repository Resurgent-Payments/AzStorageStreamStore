namespace AzStorageStreamStore;

using System.Linq;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Options;

public class SingleTenantPersister : IPersister {
    const byte NULL = 0x00;
    const byte END_OF_RECORD = 0x1E;

    IDataFileManager _dataFileManager;

    private readonly PersistenceUtils _utils;

    private readonly CancellationTokenSource _cts = new();

    private readonly Channel<StreamItem> _allStreamChannel;
    private readonly Channel<PossibleWalEntry> _streamWriterChannel;

    private readonly SingleTenantPersisterOptions _options;

    public ChannelReader<StreamItem> AllStream { get; }

    public SingleTenantPersister(IDataFileManager dataFileManager, IOptions<SingleTenantPersisterOptions> options) {
        _dataFileManager = dataFileManager;
        _options = options.Value ?? new();
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
        => ReadStreamFromAsync(id, 0);

    public async IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamId id, int startingRevision) {
        // full scan.
        if (await ReadLogAsync().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != id)) throw new StreamDoesNotExistException();

        // second full scan
        await foreach (var e in ReadLogAsync().OfType<RecordedEvent>().Where(s => s.StreamId == id).Skip(startingRevision)) {
            yield return e;
        }
    }

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamKey key)
        => ReadStreamFromAsync(key, 0);

    public async IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamKey key, int startingRevision) {
        // full scan
        await foreach (var e in ReadLogAsync().OfType<RecordedEvent>().Where(s => s.StreamId == key).Skip(startingRevision)) {
            yield return e;
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

            try {
                if (!await _utils.PassesStreamValidationAsync(onceCompleted, streamId, expected, events)) continue;

                // first full scan
                // if we don't have a StreamCreated event, we need to append one now.
                if (await ReadLogAsync().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) {
                    // write the stream created event.
                    var ms = new MemoryStream();

                    JsonSerializer.Serialize(ms, new StreamCreated(streamId), _options.JsonOptions);
                    ms.WriteByte(END_OF_RECORD);
                    await _dataFileManager.WriteAsync(ms.ToArray());
                }

                // second full scan
                var revision = (await ReadLogAsync()
                    .OfType<RecordedEvent>()
                    .Where(e => e.StreamId == streamId)
                    .LastOrDefaultAsync())?.Revision ?? -1L;

                foreach (var @event in events) {
                    revision += 1;
                    var recorded = new RecordedEvent(streamId, @event.EventId, revision, @event.Data);

                    // write the stream created event.
                    var ms = new MemoryStream();

                    JsonSerializer.Serialize(ms, recorded, _options.JsonOptions);
                    ms.WriteByte(END_OF_RECORD);
                    await _dataFileManager.WriteAsync(ms.ToArray());

                    // publish the recorded event.
                    await _allStreamChannel.Writer.WriteAsync(recorded);
                }

                onceCompleted.SetResult(WriteResult.Ok(revision));
            }
            catch (Exception exc) {
                onceCompleted.SetResult(WriteResult.Failed(-1, exc));
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

    private async IAsyncEnumerable<StreamItem> ReadLogAsync() {
        var buffer = new byte[4096];
        var ms = new MemoryStream();

        _dataFileManager.Seek(0, 0);
        int offset;
        do {
            Array.Clear(buffer);
            offset = await _dataFileManager.ReadLogAsync(buffer, buffer.Length);

            for (var idx = 0; idx < offset; idx++) {
                if (buffer[idx] == NULL) break; // if null, then no further data exists.

                if (buffer[idx] == END_OF_RECORD) { // found a point whereas we need to deserialize what we have in the buffer, yield it back to the caller, then advance the index by 1.
                    ms.Seek(0, SeekOrigin.Begin);

                    yield return JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions);

                    ms?.Dispose();
                    ms = new MemoryStream();

                    continue;
                }

                ms.WriteByte(buffer[idx]);
            }
        } while (offset != 0);

        if (ms.Length > 0) {
            yield return JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions);
        }
    }
}