namespace AzStorageStreamStore.Tests;

using System;
using System.Threading.Tasks;

using Xunit;

public abstract class StoreClientTestBase<TIStoreClient> where TIStoreClient : class, IStoreClient {
    private readonly IStoreClient _storeClient;
    private readonly StreamId _loadedStreamId = new("tenant-id", "some-id");

    public StoreClientTestBase(TIStoreClient storeClient) {
        _storeClient = storeClient;
        _storeClient.InitializeAsync().GetAwaiter().GetResult();
        _storeClient.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { new EventData(_loadedStreamId, Guid.NewGuid(), Array.Empty<byte>()) }).GetAwaiter().GetResult();
    }

    [Theory]
    [InlineData(-3L)] // no stream
    [InlineData(-2L)] // any stream
    public async Task Can_append_a_single_event(long version) {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        StreamId id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, Array.Empty<byte>());

        var writeResult = await _storeClient.AppendToStreamAsync(id, version, new[] { e });

        Assert.True(writeResult.Successful);

        var events = await _storeClient.ReadStreamAsync((StreamKey)id).ToListAsync();
        Assert.Single(events);
        Assert.Equal(id, events.First().Key);
        Assert.Equal(eventId, e.EventId);
    }

    [Fact]
    public async Task Cannot_append_when_a_stream_exists() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        StreamId id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, Array.Empty<byte>());

        var writeResult = await _storeClient.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.NoStream, new[] { e });

        Assert.False(writeResult.Successful);
        Assert.IsType<StreamExistsException>(writeResult.Exception);
    }

    [Fact]
    public async Task Appending_a_second_time_succeeds_as_a_noop() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        StreamId id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, Array.Empty<byte>());

        var writeResult1 = await _storeClient.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { e });
        var writeResult2 = await _storeClient.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { e });

        Assert.True(writeResult1.Successful);
        Assert.True(writeResult2.Successful);
    }

    [Fact]
    public async Task Appending_partial_streams_should_fail() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        StreamId id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, Array.Empty<byte>());

        var writeResult1 = await _storeClient.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { e1, e2 });
        var writeResult2 = await _storeClient.AppendToStreamAsync(_loadedStreamId, writeResult1.Version, new[] { e1 });

        Assert.True(writeResult1.Successful);
        Assert.False(writeResult2.Successful);
        Assert.IsType<WrongExpectedVersionException>(writeResult2.Exception);
    }

    [Fact]
    public async Task Can_append_to_a_stream_at_any_position() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        StreamId id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());

        var writeResult1 = await _storeClient.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e1, e2 });
        var writeResult2 = await _storeClient.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e3 });

        Assert.True(writeResult1.Successful);
        Assert.True(writeResult2.Successful);
        Assert.Equal(3, writeResult2.Position); // position is 0-based.
        Assert.Equal(2, writeResult2.Version);
    }

    [Fact]
    public async Task Can_subscribe_to_all_stream() {
        var events = new List<RecordedEvent>();
        ManualResetEventSlim _mres = new ManualResetEventSlim(false);

        _storeClient.SubscribeToAll(x => {
            events.Add(x);
            _mres.Set();
        });
        var key = new StreamId("test", "stream");

        var result = await _storeClient.AppendToStreamAsync(
            new StreamId("test", "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);


        _mres.Wait(5000);

        Assert.Single(events);
    }
}