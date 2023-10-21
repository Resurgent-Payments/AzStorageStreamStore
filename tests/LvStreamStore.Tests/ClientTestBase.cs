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

        AsyncHelper.RunSync(Client.InitializeAsync);
        var result = AsyncHelper.RunSync(async () => await Client.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { new EventData(_loadedStreamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>()) }));
        Assert.True(result.Successful);

        result = AsyncHelper.RunSync(async () => await Client.AppendToStreamAsync(_emptyStreamId, ExpectedVersion.Any, Array.Empty<EventData>()));
        Assert.True(result.Successful);
    }

    [Theory]
    [InlineData(-1L)] // no stream
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
        Assert.IsType<WrongExpectedVersionException>(writeResult.Exception);
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
    public async Task Can_subscribe_to_a_streamid_stream() {
        var streamId = new StreamId("Tenant", Array.Empty<string>(), "stream");
        var e1 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e4 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var events = new List<RecordedEvent>();

        var writeResult = await Client.AppendToStreamAsync(streamId, ExpectedVersion.NoStream, new[] { e1, e2, e3 });
        Assert.True(writeResult.Successful);

        var sub = Client.SubscribeToStream(streamId, (item) => { events.Add(item); return Task.CompletedTask; });

        await Client.AppendToStreamAsync(streamId, ExpectedVersion.Any, new[] { e4 });

        AssertEx.IsOrBecomesTrue(() => events.Count == 1, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Can_subscribe_to_a_streamkey_stream() {
        var id1 = new StreamId("tenant", Array.Empty<string>(), "object-id");
        var e1 = new EventData(new StreamId("tenant", Array.Empty<string>(), "object-1"), Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(new StreamId("tenant", Array.Empty<string>(), "object-2"), Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(new StreamId("tenant", Array.Empty<string>(), "object-3"), Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e4 = new EventData(new StreamId("tenant", Array.Empty<string>(), "object-4"), Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var key = new StreamKey(new[] { "tenant" });

        var events = new List<RecordedEvent>();

        var writeResult = await Client.AppendToStreamAsync(id1, ExpectedVersion.NoStream, new[] { e1, e2, e3 });
        Assert.True(writeResult.Successful);

        _ = Client.SubscribeToStream(key, (item) => { events.Add(item); return Task.CompletedTask; });

        await Client.AppendToStreamAsync(e4.Key, ExpectedVersion.Any, new[] { e4 });

        AssertEx.IsOrBecomesTrue(() => events.Count == 1, TimeSpan.FromSeconds(1));
    }

    //[Fact]
    //public async Task Can_subscribe_to_a_streamid_stream_at_position() {
    //    var id1 = new StreamId("Tenant", Array.Empty<string>(), "stream");
    //    var e1 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e2 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e3 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e4 = new EventData(id1, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

    //    var events = new List<RecordedEvent>();

    //    var writeResult = await Client.AppendToStreamAsync(id1, ExpectedVersion.NoStream, new[] { e1, e2, e3 });
    //    Assert.True(writeResult.Successful);

    //    _ = Client.SubscribeToStream(id1, (item) => { events.Add(item); return Task.CompletedTask; });

    //    await Client.AppendToStreamAsync(id1, ExpectedVersion.Any, new[] { e4 });

    //    Assert.Equal(2, events.Count);
    //}

    //[Fact]
    //public async Task Can_dispose_a_subscription_from_a_given_position() {
    //    var id = new StreamId("some", Array.Empty<string>(), "stream");
    //    var key = new StreamKey(new[] { "some" });
    //    var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e2 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e3 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e4 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

    //    var events = new List<RecordedEvent>();

    //    Client.SubscribeToStream(key, (item) => {
    //        events.Add(item);
    //        return Task.CompletedTask;
    //    }, 2).Dispose();

    //    var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, new[] { e1, e2, e3, e4 });
    //    Assert.True(writeResult.Successful);

    //    Assert.Empty(events);
    //}

    //[Fact]
    //public async Task Disposing_of_a_subscription_from_a_given_position_does_not_affect_other_instances_of_same() {
    //    var id = new StreamId("some", Array.Empty<string>(), "stream");
    //    var key = new StreamKey(new[] { "some" });
    //    var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e2 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e3 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
    //    var e4 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

    //    var events = new List<RecordedEvent>();


    //    Client.SubscribeToStream(key, (item) => {
    //        events.Add(item);
    //        return Task.CompletedTask;
    //    }, 2).Dispose();
    //    Client.SubscribeToStream(key, (item) => {
    //        events.Add(item);
    //        return Task.CompletedTask;
    //    }, 2);

    //    var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, new[] { e1, e2, e3, e4 });
    //    Assert.True(writeResult.Successful);

    //    Assert.Equal(2, events.Count);
    //    Assert.Equal(e3.EventId, events[0].EventId);
    //    Assert.Equal(e4.EventId, events[1].EventId);
    //}

    [Fact]
    public async Task Records_proper_stream_revision() {
        var streamId = new StreamId("some", Array.Empty<string>(), "stream");
        var e1 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e2 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e3 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());
        var e4 = new EventData(streamId, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var events = new List<RecordedEvent>();

        // need to create an empty stream to make this work.
        _ = await Client.AppendToStreamAsync(streamId, ExpectedVersion.NoStream, Array.Empty<EventData>());
        _ = Client.SubscribeToStream(streamId, (item) => {
            events.Add(item);
            return Task.CompletedTask;
        });
        var writeResult = await Client.AppendToStreamAsync(streamId, ExpectedVersion.Any, new[] { e1, e2, e3, e4 });

        Assert.True(writeResult.Successful);

        await Task.Delay(500);

        Assert.Equal(4, events.Count);
        Assert.Equal(3, events.Last().Revision);
    }

    [Fact]
    public async Task Can_append_events_when_an_empty_stream_has_been_established() {
        var id = new StreamId("some", Array.Empty<string>(), "stream");
        var e1 = new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>());

        var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, Array.Empty<EventData>());
        Assert.True(writeResult.Successful);
        await Client.AppendToStreamAsync(id, ExpectedVersion.StreamExists, new[] { e1 });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Can_subscribe_to_all_stream(int numberOfSubscriptions) {
        var events = new List<RecordedEvent>();

        var key = new StreamId("test", Array.Empty<string>(), "stream");

        for (var i = 0; i < numberOfSubscriptions; i++) {
            _ = Client.SubscribeToStream((item) => {
                events.Add(item);
                return Task.CompletedTask;
            });
        }

        var result = await Client.AppendToStreamAsync(
            new StreamId("test", Array.Empty<string>(), "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }
        );

        Assert.True(result.Successful);

        AssertEx.IsOrBecomesTrue(() => numberOfSubscriptions == events.Count, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Can_remove_an_allstream_subscription_via_returned_idsposable() {
        var events = new List<RecordedEvent>();

        Client.SubscribeToStream((item) => {
            events.Add(item);
            return Task.CompletedTask;
        }).Dispose();

        var key = new StreamId("test", Array.Empty<string>(), "stream");
        var result = await Client.AppendToStreamAsync(
            new StreamId("test", Array.Empty<string>(), "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);

        Assert.Empty(events);
    }

    [Fact]
    public async Task Disposing_an_allstream_subscription_does_not_dispose_all_subscriptions() {
        var events = new List<RecordedEvent>();

        var key = new StreamId("test", Array.Empty<string>(), "stream");


        Client.SubscribeToStream((item) => {
            events.Add(item);
            return Task.CompletedTask;
        });
        Client.SubscribeToStream((item) => {
            events.Add(item);
            return Task.CompletedTask;
        }).Dispose();

        var result = await Client.AppendToStreamAsync(
            new StreamId("test", Array.Empty<string>(), "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }
        );

        Assert.True(result.Successful);
        AssertEx.IsOrBecomesTrue(() => events.Count == 1, TimeSpan.FromSeconds(1));
    }

    //[Fact]
    //[Trait("Type", "Integration")]
    //public async Task Large_streams_will_write_and_read() {
    //    var id = new StreamId("some", Array.Empty<string>(), "stream");
    //    var fiftyGrandEventDeta = Enumerable.Range(1, 50000)
    //        .Select(_ => new EventData(id, Guid.NewGuid(), EventType, Array.Empty<byte>(), Array.Empty<byte>()))
    //        .ToArray();

    //    var writeResult = await Client.AppendToStreamAsync(id, ExpectedVersion.NoStream, fiftyGrandEventDeta);

    //    Assert.True(writeResult.Successful);

    //    var allEventsFromStorage = await Client.ReadStreamAsync(id).ToListAsync();
    //    Assert.Equal(50000, allEventsFromStorage.Count);
    //}

    public void Dispose() {
        Stream?.Dispose();
    }
}