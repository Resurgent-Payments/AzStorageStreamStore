namespace AzStorageStreamStore.Tests;

using System.Threading.Tasks;

using Xunit;

[Trait("Type", "Integration")]
public abstract class StreamClientIntegrationTestBase<TPersister> : IAsyncDisposable where TPersister : IPersister {
    protected IStoreClient Client { get; set; }
    private readonly StreamId _loadedStreamId = new("tenant-id", "some-id");
    const string integration_event_type = "integration-event";

    protected abstract TPersister Persister { get; }

    public StreamClientIntegrationTestBase() {
        Client = new LocalStoreClient(Persister);

        AsyncHelper.RunSync(async () => await Client.InitializeAsync());
        var result = AsyncHelper.RunSync(async () => await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { new EventData(_loadedStreamId, Guid.NewGuid(), integration_event_type, Array.Empty<byte>(), Array.Empty<byte>()) }));
        Assert.True(result.Successful);
    }

    [Fact]
    public async Task Large_streams_will_write_and_read() {
        var id = new StreamId("some", "stream");
        var fiftyGrandEventDeta = Enumerable.Range(1, 50000)
            .Select(_ => new EventData(id, Guid.NewGuid(), integration_event_type, Array.Empty<byte>(), Array.Empty<byte>()))
            .ToArray();

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, fiftyGrandEventDeta);

        Assert.True(writeResult.Successful);

        var allEventsFromStorage = await Client.ReadStreamAsync(id).ToListAsync();
        Assert.Equal(50000, allEventsFromStorage.Count);
    }

    public ValueTask DisposeAsync() {
        Persister?.Dispose();
        return ValueTask.CompletedTask;
    }
}