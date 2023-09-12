namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class SingleTenantDurablePersister : DurablePersisterBase, IPersister {
    private readonly string _calculatedFileName;

    public SingleTenantDurablePersister(IOptions<LocalDiskDurablePersisterOptions> options) : base(options) {
        _calculatedFileName = Path.Combine(options.Value.BaseDataPath, options.Value.DatafileName);

        if (!Directory.Exists(options.Value.BaseDataPath)) {
            Directory.CreateDirectory(options.Value.BaseDataPath);
        }

        if (!File.Exists(_calculatedFileName)) {
            File.Create(_calculatedFileName);
        }

        Task.Factory.StartNew(WriteEventsImplAsync, TokenSource.Token);
    }

    public override async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id, long position) {
        using (var file = File.OpenRead(_calculatedFileName))
        using (var stream = new StreamReader(file)) {
            for (var i = 0; i < position; i++) {
                await stream.ReadLineAsync();
            }

            string? line;
            while ((line = await stream.ReadLineAsync()) != null) {
#pragma warning disable CS8603 // Possible null reference return.
                var @event = await JsonSerializer.DeserializeAsync<RecordedEvent>(new MemoryStream(Encoding.UTF8.GetBytes(line)), Options.JsonOptions);
                if (@event.StreamId == id) {
                    yield return @event;
                }
#pragma warning restore CS8603 // Possible null reference return.
            }
        }
    }

    public override async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key, long position) {
        using (var file = File.OpenRead(_calculatedFileName))
        using (var stream = new StreamReader(file)) {
            for (var i = 0; i < position; i++) {
                await stream.ReadLineAsync();
            }

            string? line;
            while ((line = await stream.ReadLineAsync()) != null) {
#pragma warning disable CS8603 // Possible null reference return.
                var @event = await JsonSerializer.DeserializeAsync<RecordedEvent>(new MemoryStream(Encoding.UTF8.GetBytes(line)), Options.JsonOptions);
                if (@event.StreamId == key) {
                    yield return @event;
                }
#pragma warning restore CS8603 // Possible null reference return.
            }
        }
    }

    private async Task WriteEventsImplAsync() {
        await foreach (var posssibleWalEntry in WalWriter.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expectedVersion = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            if (!await ValidateValidWrite(onceCompleted, streamId, expectedVersion, events)) continue;

            // getting current position
            var position = -1L;
            var version = -1L;
            await foreach (var recorded in ReadAsync(AzStorageStreamStore.AllStream.SingleTenant)) {
                position += 1;
                if (recorded.StreamId == streamId) {
                    version += 1;
                }
            }


            try {
                using (var file = File.Open(_calculatedFileName, FileMode.Append, FileAccess.Write))
                using (var writer = new StreamWriter(file)) {
                    {
                        foreach (var data in events) {
                            version += 1;
                            var recorded = new RecordedEvent(streamId, data.EventId, version, data.Data);
                            var ser = JsonSerializer.Serialize(recorded, Options.JsonOptions);
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