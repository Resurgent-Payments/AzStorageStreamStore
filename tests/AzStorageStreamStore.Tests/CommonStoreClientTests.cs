namespace AzStorageStreamStore.Tests;

using System;
using System.Threading.Tasks;

using AzStorageStreamStore;

using FakeItEasy;

using Microsoft.Extensions.Options;

using Xunit;

public abstract class StoreClientTestBase<TPersister> : IAsyncDisposable where TPersister : IPersister {
    protected IStoreClient Client { get; set; }
    private readonly StreamId _loadedStreamId = new("tenant-id", "some-id");

    protected abstract TPersister Persister { get; }

    public StoreClientTestBase() {
        Client = new LocalStoreClient(Persister);

        AsyncHelper.RunSync(async () => await Client.InitializeAsync());
        var result = AsyncHelper.RunSync(async () => await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { new EventData(_loadedStreamId, Guid.NewGuid(), Array.Empty<byte>()) }));
        Assert.True(result.Successful);
    }

    [Theory]
    [InlineData(-3L)] // no stream
    [InlineData(-2L)] // any stream
    public async Task Can_append_a_single_event(long version) {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        var id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, Array.Empty<byte>());

        var writeResult = await Client.AppendToStreamAsync(id, version, new[] { e });

        Assert.True(writeResult.Successful);

        var events = await Client.ReadStreamAsync(id).ToListAsync();
        Assert.Single(events);
        Assert.Equal(id, events.First().StreamId);
        Assert.Equal(eventId, e.EventId);
    }

    [Fact]
    public async Task Cannot_append_when_a_stream_exists() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        var id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, Array.Empty<byte>());

        var writeResult = await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.NoStream, new[] { e });

        Assert.False(writeResult.Successful);
        Assert.IsType<StreamExistsException>(writeResult.Exception);
    }

    [Fact]
    public async Task Appending_a_second_time_succeeds_as_a_noop() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        var id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, Array.Empty<byte>());

        var writeResult1 = await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { e });
        var writeResult2 = await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { e });

        Assert.True(writeResult1.Successful);
        Assert.True(writeResult2.Successful);
    }

    [Fact]
    public async Task Appending_partial_streams_should_fail() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var eventId3 = Guid.NewGuid();
        var id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, Array.Empty<byte>());
        var e3 = new EventData(id, eventId3, Array.Empty<byte>());

        var writeResult1 = await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { e1, e2 });
        var writeResult2 = await Client.AppendToStreamAsync(_loadedStreamId, writeResult1.Version, new[] { e2, e3 });

        Assert.True(writeResult1.Successful);
        Assert.False(writeResult2.Successful);
        Assert.IsType<WrongExpectedVersionException>(writeResult2.Exception);
    }

    [Fact]
    public async Task Can_append_to_a_stream_at_any_position() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());

        var writeResult1 = await Client.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e1, e2 });
        var writeResult2 = await Client.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e3 });

        Assert.True(writeResult1.Successful);
        Assert.True(writeResult2.Successful);
    }

    [Fact]
    public async Task Can_read_a_stream_by_stream_id() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var id = new StreamId(tenantId, Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());

        await Client.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e1, e2 });
        await Client.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e3 });

        var stream = await Client.ReadStreamAsync(id).ToListAsync();

        Assert.Equal(3, stream.Count);
        Assert.Equal(e1.EventId, stream[0].EventId);
        Assert.Equal(e2.EventId, stream[1].EventId);
        Assert.Equal(e3.EventId, stream[2].EventId);
    }

    [Fact]
    public async Task Can_read_a_stream_by_stream_key() {
        var tenantId = Guid.NewGuid().ToString("N");

        var id1 = new StreamId(tenantId, Guid.NewGuid().ToString());
        var id2 = new StreamId(tenantId, Guid.NewGuid().ToString());
        var id3 = new StreamId(tenantId, Guid.NewGuid().ToString());

        var key = new StreamKey(new[] { tenantId });

        var e1 = new EventData(id1, Guid.NewGuid(), Array.Empty<byte>());
        var e2 = new EventData(id2, Guid.NewGuid(), Array.Empty<byte>());
        var e3 = new EventData(id3, Guid.NewGuid(), Array.Empty<byte>());

        await Client.AppendToStreamAsync(id1, ExpectedVersion.Any, new[] { e1 });
        await Client.AppendToStreamAsync(id2, ExpectedVersion.Any, new[] { e2 });
        await Client.AppendToStreamAsync(id3, ExpectedVersion.Any, new[] { e3 });

        await Task.Delay(1000);

        var stream = await Client.ReadStreamAsync(key).ToListAsync();

        Assert.Equal(3, stream.Count);
    }

    [Fact]
    public async Task Attempting_to_read_a_nonexistent_stream_by_id_should_throw_stream_does_not_exist_exception() {
        var id = new StreamId(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
        Assert.Throws<StreamDoesNotExistException>(() => AsyncHelper.RunSync(async () => await Client.ReadStreamAsync(id).ToListAsync()));
    }

    [Fact]
    public async Task Attempting_to_read_a_nonexistent_stream_by_key_should_return_no_results() {
        var key = new StreamKey(new[] { Guid.NewGuid().ToString() });
        var stream = await Client.ReadStreamAsync(key).ToListAsync();
        Assert.Empty(stream);
    }

    [Fact]
    public async Task Can_subscribe_to_a_stream_from_a_given_position() {
        var id1 = new StreamId("Tenant", "stream");
        var e1 = new EventData(id1, Guid.NewGuid(), Array.Empty<byte>());
        var e2 = new EventData(id1, Guid.NewGuid(), Array.Empty<byte>());
        var e3 = new EventData(id1, Guid.NewGuid(), Array.Empty<byte>());
        var e4 = new EventData(id1, Guid.NewGuid(), Array.Empty<byte>());

        var events = new List<RecordedEvent>();

        var writeResult = await Client.AppendToStreamAsync(id1, ExpectedVersion.NoStream, new[] { e1, e2, e3 });
        Assert.True(writeResult.Successful);

        var mre = new ManualResetEventSlim(false);
        var waitingToSet = 4;
        var numberOfEvents = 0;
        var subscription = await Client.SubscribeToStreamFromAsync(id1, 0, e => {
            if (numberOfEvents >= waitingToSet) return;

            events.Add(e);
            numberOfEvents += 1;

            if (numberOfEvents >= waitingToSet) {
                mre.Set();
            }
        });

        await Client.AppendToStreamAsync(id1, ExpectedVersion.Any, new[] { e4 });

        mre.Wait(500);

        Assert.Equal(4, events.Count);
    }

    [Fact]
    public async Task Can_dispose_a_subscription_from_a_given_position() {
        var id = new StreamId("some", "stream");
        var key = new StreamKey(new[] { "some" });
        var e1 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e2 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e4 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());

        var events = new List<RecordedEvent>();

        var mre = new ManualResetEventSlim(false);
        var waitingToSet = 1;
        var numberOfEvents = 0;

        (await Client.SubscribeToStreamFromAsync(key, 2, e => {
            events.Add(e);
            numberOfEvents += 1;
            if (numberOfEvents >= waitingToSet) {
                mre.Set();
            }
        })).Dispose();

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, new[] { e1, e2, e3, e4 });
        Assert.True(writeResult.Successful);

        mre.Wait(100);
        Assert.Empty(events);
    }

    [Fact]
    public async Task Disposing_of_a_subscription_from_a_given_position_does_not_affect_other_instances_of_same() {
        var id = new StreamId("some", "stream");
        var key = new StreamKey(new[] { "some" });
        var e1 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e2 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e4 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());

        var events = new List<RecordedEvent>();

        var mre = new ManualResetEventSlim(false);
        var waitingToSet = 4;
        var numberOfEvents = 0;

        (await Client.SubscribeToStreamFromAsync(key, 2, e => {
            // nothing to do
        })).Dispose();
        await Client.SubscribeToStreamFromAsync(key, 2, e => {
            events.Add(e);
            numberOfEvents += 1;
            if (numberOfEvents >= waitingToSet) {
                mre.Set();
            }
        });

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, new[] { e1, e2, e3, e4 });
        Assert.True(writeResult.Successful);

        mre.Wait(100);
        Assert.Equal(4, events.Count);
    }

    [Fact]
    public async Task Records_proper_stream_revision() {
        var id = new StreamId("some", "stream");
        var e1 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e2 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());
        var e4 = new EventData(id, Guid.NewGuid(), Array.Empty<byte>());

        var events = new List<RecordedEvent>();

        // need to create an empty stream to make this work.
        var mre = new ManualResetEventSlim(false);
        var expectedEvents = 4;
        await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, Array.Empty<EventData>());
        await Client.SubscribeToStreamAsync(id, e => {
            events.Add(e);
            if (events.Count >= expectedEvents) {
                mre.Set();
            }
        });

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e1, e2, e3, e4 });

        mre.Wait(1000);

        Assert.True(writeResult.Successful);
        Assert.Equal(4, events.Count);
        Assert.Equal(3, events.Last().Revision);
    }

    public ValueTask DisposeAsync() {
        Persister?.Truncate();
        Persister?.Dispose();
        return ValueTask.CompletedTask;
    }
}