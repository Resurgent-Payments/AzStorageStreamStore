namespace AzStorageStreamStore.Tests;

using System.Diagnostics;
using System.Text;
using System.Text.Json;

using Xunit;

public class StreamItemJsonConverterTests {
    StreamItemJsonConverter _sut;
    SingleTenantPersisterOptions _options;

    public StreamItemJsonConverterTests() {
        _sut = new StreamItemJsonConverter();
        _options = new();
    }

    [Theory]
    [InlineData(typeof(RecordedEvent))]
    [InlineData(typeof(StreamCreated))]
    public void Can_convert_type(Type type) {
        Assert.True(_sut.CanConvert(type));
    }

    [Fact]
    public void Stream_created_event_type_can_be_serialized() {
        var @event = new StreamCreated(new StreamId("stream", "id"));
        var ms = new MemoryStream();

        JsonSerializer.Serialize(ms, @event, _options.JsonOptions);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<StreamCreated>(ms, _options.JsonOptions);

        Assert.Equivalent(@event, deserialized);
    }

    [Fact]
    public void Stream_id_can_be_serialized() {
        var key = new StreamId("stream", "id");
        var ms = new MemoryStream();

        JsonSerializer.Serialize(ms, key, _options.JsonOptions);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<StreamId>(ms, _options.JsonOptions);

        Assert.Equivalent(key, deserialized);
    }

    [Fact]
    public void Recorded_event_without_data_can_be_serialized() {
        var key = new StreamId("stream", "id");
        var eventId = Guid.NewGuid();
        var @event = new RecordedEvent(key, eventId, 1, "type", Array.Empty<byte>(), Array.Empty<byte>());

        var ms = new MemoryStream();

        JsonSerializer.Serialize(ms, @event, _options.JsonOptions);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<RecordedEvent>(ms, _options.JsonOptions);

        Assert.Equivalent(@event, deserialized);
    }

    [Fact]
    public void Recorded_event_with_data_can_be_serialized() {
        var key = new StreamId("stream", "id");
        var eventId = Guid.NewGuid();
        var @event = new RecordedEvent(key, eventId, 1, "type", Array.Empty<byte>(), new byte[] { 1, 2, 3, 4 });

        var ms = new MemoryStream();

        JsonSerializer.Serialize(ms, @event, _options.JsonOptions);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<RecordedEvent>(ms, _options.JsonOptions);

        Assert.Equivalent(@event, deserialized);
    }

    [Fact]
    public void Recorded_event_with_metadata_can_be_serialized() {
        var key = new StreamId("stream", "id");
        var eventId = Guid.NewGuid();
        var @event = new RecordedEvent(key, eventId, 1, "type", new byte[] { 1, 2, 3, 4 }, Array.Empty<byte>());

        var ms = new MemoryStream();

        JsonSerializer.Serialize(ms, @event, _options.JsonOptions);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<RecordedEvent>(ms, _options.JsonOptions);

        Assert.Equivalent(@event, deserialized);
    }
}