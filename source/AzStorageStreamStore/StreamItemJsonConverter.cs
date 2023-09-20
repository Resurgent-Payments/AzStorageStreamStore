namespace AzStorageStreamStore;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StreamItemJsonConverter : JsonConverter<StreamItem> {
    const string EventType = nameof(EventType);

    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsAssignableTo(typeof(StreamItem));
    }

    public override StreamItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var cloned = reader;

        if (cloned.TokenType != JsonTokenType.StartObject) throw new JsonException();

        var propertyName = "";
        do {
            cloned.Read();
            if (cloned.TokenType == JsonTokenType.PropertyName && cloned.GetString() == EventType) {
                propertyName = cloned.GetString() ?? string.Empty;
                cloned.Read();
                break;
            }
        } while (cloned.TokenType != JsonTokenType.EndObject || propertyName.Equals(EventType));

        if (!propertyName.Equals(EventType)) throw new JsonException();

        if (cloned.TokenType != JsonTokenType.Number) throw new JsonException();

        var discriminator = (StreamItemTypes)cloned.GetInt32();

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
        //TODO: Ask if this is a legit warning "silence" item.
        StreamItem item = discriminator switch {
            StreamItemTypes.StreamCreated => DeserializeStreamCreated(ref reader, options),
            StreamItemTypes.RecordedEvent => DeserializeRecordedEvent(ref reader, options),
            _ => throw new JsonException()
        };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        return item;
    }

    public override void Write(Utf8JsonWriter writer, StreamItem value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        switch (value) {
            case StreamCreated created:
                writer.WriteNumber(EventType, (int)StreamItemTypes.StreamCreated);

                writer.WritePropertyName(options?.PropertyNamingPolicy?.ConvertName("StreamId") ?? "StreamId");
                JsonSerializer.Serialize(writer, created.StreamId, options);

                break;
            case RecordedEvent @event:
                writer.WriteNumber(EventType, (int)StreamItemTypes.RecordedEvent);

                writer.WritePropertyName(options?.PropertyNamingPolicy?.ConvertName("StreamId") ?? "StreamId");
                JsonSerializer.Serialize(writer, @event.StreamId, options);

                writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.EventId)) ?? nameof(@event.EventId), @event.EventId);
                writer.WriteNumber(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.Revision)) ?? nameof(@event.Revision), @event.Revision);
                //todo: determine how to write a byte array here.  can this be a json text at the end?
                writer.WriteBase64String(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.Data)) ?? nameof(@event.Data), @event.Data);

                break;
            default:
                throw new JsonException();
        }

        writer.WriteEndObject();
    }

    private static StreamCreated DeserializeStreamCreated(ref Utf8JsonReader reader, JsonSerializerOptions options) {
        // find the StreamId node
        StreamCreated? created = null;

        do {
            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var nameOfProperty = reader.GetString();
            nameOfProperty = string.IsNullOrEmpty(nameOfProperty) ? nameOfProperty : options?.PropertyNamingPolicy?.ConvertName(nameOfProperty) ?? nameOfProperty;

            var expectedNameOfProperty = options?.PropertyNamingPolicy?.ConvertName("StreamId") ?? "StreamId";

            if (!string.IsNullOrWhiteSpace(nameOfProperty) && nameOfProperty.Equals(expectedNameOfProperty)) {
                reader.Read();
                var streamId = JsonSerializer.Deserialize<StreamId>(ref reader, options);
                if (streamId is null) throw new JsonException();
                created = new StreamCreated(streamId);
                reader.Read();
            }
        } while (reader.TokenType != JsonTokenType.EndObject);

        return created ?? throw new JsonException();
    }

    private static RecordedEvent DeserializeRecordedEvent(ref Utf8JsonReader reader, JsonSerializerOptions options) {
        StreamId? StreamId = null;
        Guid EventId = Guid.Empty;
        long Revision = -1;
        byte[] Data = null;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = options?.PropertyNamingPolicy?.ConvertName(reader.GetString() ?? string.Empty) ?? reader.GetString();
            if (string.IsNullOrEmpty(propertyName)) continue;

            reader.Read(); // now at the value.

            if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(StreamId)) ?? nameof(StreamId))) {
                StreamId = JsonSerializer.Deserialize<StreamId>(ref reader, options);
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(EventId)) ?? nameof(EventId))) {
                var guidString = reader.GetString();
                Guid.TryParse(guidString, out EventId);
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(Revision)) ?? nameof(Revision))) {
                Revision = reader.GetInt64();
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(Data)) ?? nameof(Data))) {
                Data = reader.GetBytesFromBase64();
            }
        }

        if (StreamId is null || EventId == Guid.Empty || Revision < 0) throw new JsonException();

        return new RecordedEvent(StreamId, EventId, Revision, Data);
    }

    enum StreamItemTypes {
        Unknown = 0,
        StreamCreated = 1,
        RecordedEvent = 2,
    }
}