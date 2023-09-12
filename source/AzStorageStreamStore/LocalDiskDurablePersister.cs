namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class SingleTenantDurablePersister : IPersister {
    private readonly CancellationTokenSource _tokenSource = new();
    private readonly LocalDiskDurablePersisterOptions _options;
    private string _chunkFile => Path.Combine(_options.BaseDataPath, "chunk.dat");
    private Channel<PossibleWalEntry> _walWriter = Channel.CreateUnbounded<PossibleWalEntry>(new UnboundedChannelOptions {
        SingleReader = true,
        SingleWriter = false
    });
    private Channel<RecordedEvent> _allStream = Channel.CreateUnbounded<RecordedEvent>(new UnboundedChannelOptions {
        SingleReader = true,
        SingleWriter = true
    });

    public SingleTenantDurablePersister(IOptions<LocalDiskDurablePersisterOptions> options) {
        _options = options.Value ?? new();
        _tokenSource.Token.Register(() => _walWriter.Writer.Complete());
        _tokenSource.Token.Register(() => _allStream.Writer.Complete());
        Task.Factory.StartNew(WriteEventsImplAsync, _tokenSource.Token);
    }

    public ChannelReader<RecordedEvent> AllStream => _allStream.Reader;

    public void Dispose() {
        _tokenSource.Dispose();
        GC.SuppressFinalize(this);
    }

    public IAsyncEnumerable<RecordedEvent> ReadAllAsync()
        => ReadAllAsync(0);

    public async IAsyncEnumerable<RecordedEvent> ReadAllAsync(long position) {
        using (var file = File.OpenRead(_chunkFile))
        using (var stream = new StreamReader(file)) {
            string? line;
            while ((line = await stream.ReadLineAsync()) != null) {
#pragma warning disable CS8603 // Possible null reference return.
                yield return await JsonSerializer.DeserializeAsync<RecordedEvent>(new MemoryStream(Encoding.UTF8.GetBytes(line)), _options.JsonOptions);
#pragma warning restore CS8603 // Possible null reference return.
            }
        }
    }

    public async IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id, long position) {
        var currentPosition = -1;
        await foreach (var @event in ReadAllAsync()) {
            currentPosition += 1;
            if (currentPosition < position) continue;
            if (@event.StreamId == id)
                yield return @event;
        }
    }

    public async IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key, long position) {
        var currentPosition = -1;
        await foreach (var @event in ReadAllAsync()) {
            currentPosition += 1;
            if (currentPosition < position) continue;
            if (@event.StreamId == key)
                yield return @event;
        }
    }

    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id)
        => ReadStreamAsync(id, 0);

    public IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key)
        => ReadStreamAsync(key, 0);

    public async ValueTask<WriteResult> AppendToStreamAsync(StreamId id, ExpectedVersion version, EventData[] events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _walWriter.Writer.WriteAsync(new PossibleWalEntry(tcs, id, version, events));
        return await tcs.Task;
    }

    private async Task WriteEventsImplAsync() {
        await foreach (var posssibleWalEntry in _walWriter.Reader.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expectedVersion = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            switch (expectedVersion) {
                case -3: // no stream
                    if (!await ReadAllAsync().AllAsync(e => e.StreamId != streamId)) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, -1, new StreamExistsException()));
                        continue;
                    }
                    break;
                case -2: // any stream
                case -1: // empty stream
                default:
                    long eventsInStream = 0;
                    var streamEvents = await ReadAllAsync().Where(@event => @event.StreamId == streamId).ToListAsync();

                    if (!streamEvents.Any()) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, -1, new WrongExpectedVersionException(expectedVersion, ExpectedVersion.NoStream)));
                        continue;
                    }

                    if (streamEvents.Count != expectedVersion) {
                        // if all events are appended, considered as a double request and post-back ok.
                        if (!streamEvents.All(e => events.All(i => e.EventId != i.EventId))) {
                            onceCompleted.SetResult(WriteResult.Ok(-1, streamEvents.Max(x => x.Revision)));
                            continue;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => streamEvents.All(s => s.EventId != e.EventId)).Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(-1,
                                -1,
                                new WrongExpectedVersionException(expectedVersion, streamEvents.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                            continue;
                        }
                    }
                    break;
            }

            // getting current position
            var position = -1L;
            var version = -1L;
            await foreach (var recorded in ReadAllAsync()) {
                position += 1;
                if (recorded.StreamId == streamId) {
                    version += 1;
                }
            }


            try {
                using (var file = File.Open(_chunkFile, FileMode.Append, FileAccess.Write))
                using (var writer = new StreamWriter(file)) {
                    {
                        foreach (var data in events) {
                            version += 1;
                            var recorded = new RecordedEvent(streamId, data.EventId, version, data.Data);
                            var ser = JsonSerializer.Serialize(recorded, _options.JsonOptions);
                            await writer.WriteLineAsync(ser);
                        }
                    }
                }
            }
            catch (Exception ex) {
                onceCompleted.SetException(ex);
                continue;
            }

            onceCompleted.SetResult(WriteResult.Ok(position, version));
        }
    }
}