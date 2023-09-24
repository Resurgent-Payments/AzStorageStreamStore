namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class SingleTenantOnDiskPersister : IPersister {
    private readonly PersistenceUtils _utils;
    private readonly CancellationTokenSource _tokenSource = new();
    private readonly SingleTenantOnDiskPersisterOptions _options;
    private string _chunkFile => Path.Combine(_options.BaseDataPath, "chunk.dat");
    private Channel<PossibleWalEntry> _walWriter = Channel.CreateUnbounded<PossibleWalEntry>(new UnboundedChannelOptions {
        SingleReader = true,
        SingleWriter = false
    });
    private Channel<StreamItem> _allStream = Channel.CreateUnbounded<StreamItem>(new UnboundedChannelOptions {
        SingleReader = true,
        SingleWriter = true
    });

    public SingleTenantOnDiskPersister(IOptions<SingleTenantOnDiskPersisterOptions> options) {
        _options = options.Value ?? new();
        _utils = new(this);

        if (!Directory.Exists(_options.BaseDataPath)) {
            Directory.CreateDirectory(_options.BaseDataPath);
        }

        if (!File.Exists(_chunkFile))
            File.Create(_chunkFile).Dispose();

        _tokenSource.Token.Register(() => _walWriter.Writer.Complete());
        _tokenSource.Token.Register(() => _allStream.Writer.Complete());
        Task.Factory.StartNew(WriteEventsImplAsync, _tokenSource.Token);
    }

    public ChannelReader<StreamItem> AllStream => _allStream.Reader;

    public void Dispose() {
        _tokenSource.Dispose();
        GC.SuppressFinalize(this);
    }

    public async IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamId id, int start) {
        if (await ReadAllAsync().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != id)) throw new StreamDoesNotExistException();


        await foreach (var @event in ReadAllAsync().OfType<RecordedEvent>().Where(@event => @event.StreamId == id && @event.Revision >= start)) {
            yield return @event;
        }
    }

    public async IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamKey key, int start) {
        var currentPosition = -1;
        await foreach (var @event in ReadAllAsync()) {
            currentPosition += 1;
            if (currentPosition < start) continue;
            if (@event.StreamId == key)
                yield return @event;
        }
    }

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamId id)
        => ReadStreamFromAsync(id, 0);

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamKey key)
        => ReadStreamFromAsync(key, 0);

    public async ValueTask<WriteResult> AppendToStreamAsync(StreamId id, ExpectedVersion version, EventData[] events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _walWriter.Writer.WriteAsync(new PossibleWalEntry(tcs, id, version, events));
        return await tcs.Task;
    }

    public ValueTask Truncate() {
        var files = Directory.GetFiles(_options.BaseDataPath, "*", SearchOption.AllDirectories);
        foreach (var file in files) {
            File.Delete(file);
        }
        return ValueTask.CompletedTask;
    }

    private IAsyncEnumerable<StreamItem> ReadAllAsync()
        => ReadAllAsync(0);

    private async IAsyncEnumerable<StreamItem> ReadAllAsync(long position) {
        using (var stream = new StreamReader(_chunkFile, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Options = FileOptions.Asynchronous, Share = FileShare.ReadWrite })) {
            string? line;
            while ((line = await stream.ReadLineAsync()) != null) {
#pragma warning disable CS8603 // Possible null reference return.
                yield return await JsonSerializer.DeserializeAsync<StreamItem>(new MemoryStream(Encoding.UTF8.GetBytes(line)), _options.JsonOptions);
#pragma warning restore CS8603 // Possible null reference return.
            }
        }
    }

    private async Task WriteEventsImplAsync() {
        await foreach (var posssibleWalEntry in _walWriter.Reader.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expected = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            try {
                if (!await _utils.PassesStreamValidationAsync(onceCompleted, streamId, expected, events)) continue;

                // getting current position
                long version = -1;
                try {
                    version = await ReadAllAsync()
                        .OfType<RecordedEvent>()
                        .Where(x => x.StreamId == streamId)
                        .MaxAsync(x => x.Revision);
                }
                catch (Exception exc) {
                    // squelch, hopefully.
                }

                bool needsCreatedEvent = await ReadAllAsync().OfType<StreamCreated>().AllAsync((@event) => @event.StreamId != streamId);

                using (var writer = new StreamWriter(_chunkFile, new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Append, Options = FileOptions.Asynchronous, Share = FileShare.Read })) {
                    if (needsCreatedEvent) {
                        var createdEvent = new StreamCreated(streamId);
                        var created = JsonSerializer.Serialize(createdEvent, _options.JsonOptions);
                        await writer.WriteLineAsync(created);
                    }
                    foreach (var data in events) {
                        version += 1;
                        var recorded = new RecordedEvent(streamId, data.EventId, version, data.Data);
                        var ser = JsonSerializer.Serialize(recorded, _options.JsonOptions);
                        await writer.WriteLineAsync(ser);

                        await _allStream.Writer.WriteAsync(recorded);
                    }

                    await writer.FlushAsync();
                }

                onceCompleted.SetResult(WriteResult.Ok(version));
            }
            catch (Exception ex) {
                onceCompleted.SetException(ex);
                continue;
            }
        }
    }
}