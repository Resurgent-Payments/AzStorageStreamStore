namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class SingleTenantDurablePersister : DurablePersisterBase, IPersister {

    public SingleTenantDurablePersister(IOptions<LocalDiskDurablePersisterOptions> options) : base(options) {
        if (!Directory.Exists(options.Value.BaseDataPath)) {
            Directory.CreateDirectory(options.Value.BaseDataPath);
        }

        if (!File.Exists(CalculateFileName(AllStreamId.SingleTenant))) {
            File.Create(CalculateFileName(AllStreamId.SingleTenant), 1024).Dispose();
        }

        Task.Factory.StartNew(WriteEventsImplAsync, TokenSource.Token);
    }

    public override async IAsyncEnumerable<RecordedEvent> ReadAllAsync(long fromPosition) {
        using (var file = File.OpenRead(CalculateFileName(AllStreamId.SingleTenant)))
        using (var stream = new StreamReader(file)) {
            for (var i = 0; i < fromPosition; i++) {
                await stream.ReadLineAsync();
            }

            string? line;
            while ((line = await stream.ReadLineAsync()) != null) {
#pragma warning disable CS8603 // Possible null reference return.
                yield return await JsonSerializer.DeserializeAsync<RecordedEvent>(new MemoryStream(Encoding.UTF8.GetBytes(line)), Options.JsonOptions);
#pragma warning restore CS8603 // Possible null reference return.
            }
        }
    }

    public override async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id, long position) {
        await foreach (var @event in ReadAllAsync()) {
            if (@event.StreamId == id) yield return @event;
        }
    }

    public override async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key, long position) {
        await foreach (var @event in ReadAllAsync()) {
            if (@event.StreamId == key) yield return @event;
        }
    }

    private async Task WriteEventsImplAsync() {
        await foreach (var posssibleWalEntry in WalWriter.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expectedVersion = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            if (!await ValidateValidWrite(onceCompleted, streamId, expectedVersion, events)) continue;

            // create wal file.
            onceCompleted.SetResult(WriteResult.Ok(Position, -1));

            // send signal for wal writer to append to log.
        }
    }
}