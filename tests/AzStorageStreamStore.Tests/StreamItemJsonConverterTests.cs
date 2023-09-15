namespace AzStorageStreamStore.Tests;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Xunit;

public class StreamItemJsonConverterTests {
    StreamItemJsonConverter _sut;

    public StreamItemJsonConverterTests() {
        _sut = new StreamItemJsonConverter();
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
        var options = new JsonSerializerOptions {
            Converters = {
                _sut,
                new StreamIdJsonConverter()
            },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false
        };

        JsonSerializer.Serialize(ms, @event, options);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<StreamCreated>(ms, options);

        Assert.Equivalent(@event, deserialized);
    }

    [Fact]
    public void Stream_id_can_be_serialized() {
        var key = new StreamId("stream", "id");
        var ms = new MemoryStream();
        var options = new JsonSerializerOptions {
            Converters = {
                _sut,
                new StreamIdJsonConverter()
            },
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false
        };

        JsonSerializer.Serialize(ms, key, options);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<StreamId>(ms, options);

        Assert.Equivalent(key, deserialized);
    }

    [Fact]
    public void Recorded_event_without_data_can_be_serialized() {
        var key = new StreamId("stream", "id");
        var eventId = Guid.NewGuid();
        var @event = new RecordedEvent(key, eventId, 1, Array.Empty<byte>());

        var ms = new MemoryStream();
        var options = new JsonSerializerOptions {
            Converters = {
                _sut,
                new StreamIdJsonConverter()
            },
            IgnoreReadOnlyFields = false,
            IgnoreReadOnlyProperties = false
        };

        JsonSerializer.Serialize(ms, @event, options);

        ms.Seek(0, SeekOrigin.Begin);
        Debug.WriteLine(Encoding.UTF8.GetString(ms.GetBuffer()));
        ms.Seek(0, SeekOrigin.Begin);

        var deserialized = JsonSerializer.Deserialize<RecordedEvent>(ms, options);

        Assert.Equivalent(@event, deserialized);
    }
}