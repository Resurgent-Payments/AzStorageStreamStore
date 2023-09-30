namespace LvStreamStore.Tests;

using System;
using System.Text;
using System.Threading.Tasks;

using Xunit;

public abstract class ClientTestBase : IDisposable {
    private readonly StreamId _loadedStreamId = new("tenant-id", Array.Empty<string>(), "some-id");
    private readonly StreamId _emptyStreamId = new("empty-id", Array.Empty<string>(), "stream-id");

    const string EventType = "event-type";
    const string AllStreamEventType = "$all";

    protected abstract EventStream Stream { get; }
    public IEventStreamClient Client { get; }

    public ClientTestBase() {
        Client = new InProcessEventStreamClient(Stream);

        AsyncHelper.RunSync(async () => await Client.InitializeAsync());
        var result = AsyncHelper.RunSync(async () => await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { new EventData(_loadedStreamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>()) }));
        Assert.True(result.Successful);

        result = AsyncHelper.RunSync(async () => await Client.AppendToStreamAsync(_emptyStreamId, ExpectedVersion.Any, Array.Empty<EventData>()));
        Assert.True(result.Successful);
    }

    [Theory]
    [InlineData(-3L)] // no stream
    [InlineData(-2L)] // any stream
    public async Task Can_append_a_single_event(long version) {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        var id = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var writeResult = await Client.AppendToStreamAsync(id, version, new[] { e });

        Assert.True(writeResult.Successful);

        var events = await Client.ReadStreamAsync(id).ToListAsync();
        Assert.Single(events);
        Assert.Equal(id, events.First().StreamId);
        Assert.Equal(eventId, e.EventId);
    }

    [Fact]
    public async Task Can_append_events_with_data() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        var id = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());
        var message = "Hello world!";

        var e = new EventData(id, eventId, EventType, Array.Empty<byte>(), Encoding.UTF8.GetBytes(message));

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, new[] { e });

        Assert.True(writeResult.Successful);

        var @events = await Client.ReadStreamAsync(id).ToListAsync();
        Assert.Single(@events);
        var @event = @events.First();
        Assert.Equal(id, @event.StreamId);
        Assert.Equal(message, Encoding.UTF8.GetString(@event.Data));
    }

    [Fact]
    public async Task Cannot_append_events_when_expecting_a_nonexistent_stream() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        var id = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var writeResult = await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.NoStream, new[] { e });

        Assert.False(writeResult.Successful);
        Assert.IsType<StreamExistsException>(writeResult.Exception);
    }

    [Fact]
    public async Task Duplicate_appends_render_a_noop_after_the_first_request() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId = Guid.NewGuid();
        var id = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());

        var e = new EventData(id, eventId, EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var writeResult1 = await Client.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e });
        var writeResult2 = await Client.AppendToStreamAsync(id, ExpectedVersion.Any, new[] { e });

        Assert.True(writeResult1.Successful);
        Assert.True(writeResult2.Successful);
    }

    [Fact]
    public async Task Appending_partial_streams_should_fail() {
        var tenantId = Guid.NewGuid().ToString("N");
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();
        var eventId3 = Guid.NewGuid();
        var id = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id, eventId3, EventType, Array.Empty<byte>(), Array.Empty<byte>());

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
        var id = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

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
        var id = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());

        var e1 = new EventData(id, eventId1, EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id, eventId2, EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

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

        var id1 = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());
        var id2 = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());
        var id3 = new StreamId(tenantId, Array.Empty<string>(), Guid.NewGuid().ToString());

        var key = new StreamKey(new[] { tenantId });

        var e1 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id2, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id3, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

        await Client.AppendToStreamAsync(id1, ExpectedVersion.Any, new[] { e1 });
        await Client.AppendToStreamAsync(id2, ExpectedVersion.Any, new[] { e2 });
        await Client.AppendToStreamAsync(id3, ExpectedVersion.Any, new[] { e3 });

        await Task.Delay(1000);

        var stream = await Client.ReadStreamAsync(key).ToListAsync();

        Assert.Equal(3, stream.Count);
    }

    [Fact]
    public async Task Attempting_to_read_a_nonexistent_stream_by_id_should_throw_StreamDoesNotExistException() {
        var id = new StreamId(Guid.NewGuid().ToString(), Array.Empty<string>(), Guid.NewGuid().ToString());
        await Assert.ThrowsAsync<StreamDoesNotExistException>(async () => await Client.ReadStreamAsync(id).ToListAsync());
    }

    [Fact]
    public async Task Reading_an_empty_stream_by_id_should_return_no_events() {
        List<RecordedEvent> events = null;
        var exc = await Record.ExceptionAsync(async () => events = await Client.ReadStreamAsync(_emptyStreamId).ToListAsync());
        Assert.Null(exc);
        Assert.Empty(events);
    }

    [Fact]
    public async Task Attempting_to_read_a_nonexistent_stream_by_key_should_return_no_results() {
        var key = new StreamKey(new[] { Guid.NewGuid().ToString() });
        var stream = await Client.ReadStreamAsync(key).ToListAsync();
        Assert.Empty(stream);
    }

    [Fact]
    public async Task Can_subscribe_to_a_stream_from_a_given_position() {
        var id1 = new StreamId("Tenant", Array.Empty<string>(), "stream");
        var e1 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e4 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

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
        var id = new StreamId("some", Array.Empty<string>(), "stream");
        var key = new StreamKey(new[] { "some" });
        var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e4 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

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
        var id = new StreamId("some", Array.Empty<string>(), "stream");
        var key = new StreamKey(new[] { "some" });
        var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e4 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

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
        var id = new StreamId("some", Array.Empty<string>(), "stream");
        var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e4 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

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

    [Fact]
    public async Task Cannot_append_when_no_stream_exists_while_expecting_empty() {
        var id = new StreamId("some", Array.Empty<string>(), "stream");
        var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var result = await Client.AppendToStreamAsync(id, ExpectedVersion.EmptyStream, new[] { e1 });
        Assert.IsType<WrongExpectedVersionException>(result.Exception);
    }

    [Fact]
    public async Task Can_append_events_when_an_empty_stream_has_been_established() {
        var id = new StreamId("some", Array.Empty<string>(), "stream");
        var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, Array.Empty<EventData>());
        Assert.True(writeResult.Successful);
        await Client.AppendToStreamAsync(id, ExpectedVersion.EmptyStream, new[] { e1 });
    }

    [Fact]
    public async Task Can_subscribe_to_all_stream() {
        var events = new List<RecordedEvent>();
        var _mres = new ManualResetEventSlim(false);

        await Client.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres.Set();
        });
        var key = new StreamId("test", Array.Empty<string>(), "stream");

        var result = await Client.AppendToStreamAsync(
            new StreamId("test", Array.Empty<string>(), "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);


        _mres.Wait(500);

        Assert.Single(events);
    }

    [Fact]
    public async Task Can_subscribe_to_all_stream_multiple_times() {
        var events = new List<RecordedEvent>();

        var _mres1 = new ManualResetEventSlim(false);
        var _mres2 = new ManualResetEventSlim(false);

        await Client.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres1.Set();
        });
        await Client.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres2.Set();
        });

        var key = new StreamId("test", Array.Empty<string>(), "stream");

        var result = await Client.AppendToStreamAsync(
            new StreamId("test", Array.Empty<string>(), "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);


        _mres1.Wait(200);
        _mres2.Wait(200);

        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task Can_remove_an_allstream_subscription_via_returned_idsposable() {
        var events = new List<RecordedEvent>();
        var _mres = new ManualResetEventSlim(false);

        var disposer = await Client.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres.Set();
        });
        disposer.Dispose();

        var key = new StreamId("test", Array.Empty<string>(), "stream");
        var result = await Client.AppendToStreamAsync(
            new StreamId("test", Array.Empty<string>(), "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);

        _mres.Wait(100);

        Assert.Empty(events);
    }

    [Fact]
    public async Task Disposing_an_allstream_subscription_does_not_dispose_all_subscriptions() {
        var events = new List<RecordedEvent>();

        var _mres1 = new ManualResetEventSlim(false);
        var _mres2 = new ManualResetEventSlim(false);

        await Client.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres1.Set();
        });

        // create and immedialy dispose to loop the subscription/disposal pipeline.
        (await Client.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres2.Set();
        })).Dispose();

        var key = new StreamId("test", Array.Empty<string>(), "stream");

        var result = await Client.AppendToStreamAsync(
            new StreamId("test", Array.Empty<string>(), "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);


        _mres1.Wait(100);
        _mres2.Wait(100);

        Assert.Single(events);
    }

    [Fact]
    [Trait("Type", "Integration")]
    public async Task Large_streams_will_write_and_read() {
        var id = new StreamId("some", Array.Empty<string>(), "stream");
        var fiftyGrandEventDeta = Enumerable.Range(1, 50000)
            .Select(_ => new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>()))
            .ToArray();

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, fiftyGrandEventDeta);

        Assert.True(writeResult.Successful);

        var allEventsFromStorage = await Client.ReadStreamAsync(id).ToListAsync();
        Assert.Equal(50000, allEventsFromStorage.Count);
    }

    public void Dispose() {
        Stream?.Dispose();
    }
}