namespace LvStreamStore;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class StreamItemJsonConverter : JsonConverter<StreamMessage> {
    const string EventType = nameof(EventType);

    public override bool CanConvert(Type typeToConvert) {
        return typeToConvert.IsAssignableTo(typeof(StreamMessage));
    }

    public override StreamMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
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
        StreamMessage item = discriminator switch {
            StreamItemTypes.StreamCreated => DeserializeStreamCreated(ref reader, options),
            StreamItemTypes.RecordedEvent => DeserializeRecordedEvent(ref reader, options),
            _ => throw new JsonException()
        };
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
        return item;
    }

    public override void Write(Utf8JsonWriter writer, StreamMessage value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        switch (value) {
            case StreamCreated created:
                writer.WriteNumber(EventType, (int)StreamItemTypes.StreamCreated);

                writer.WritePropertyName(options?.PropertyNamingPolicy?.ConvertName("StreamId") ?? "StreamId");
                JsonSerializer.Serialize(writer, created.StreamId, options);
                writer.WriteNumber(options?.PropertyNamingPolicy?.ConvertName(nameof(created.Position)) ?? nameof(created.Position), created.Position);
                writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(created.MsgId)) ?? nameof(created.MsgId), created.MsgId!.Value);
                break;
            case RecordedEvent @event:
                writer.WriteNumber(EventType, (int)StreamItemTypes.RecordedEvent);

                writer.WritePropertyName(options?.PropertyNamingPolicy?.ConvertName("StreamId") ?? "StreamId");
                JsonSerializer.Serialize(writer, @event.StreamId, options);

                writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.EventId)) ?? nameof(@event.EventId), @event.EventId);
                writer.WriteNumber(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.Position)) ?? nameof(@event.Position), @event.Position);
                writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.Type)) ?? nameof(@event.Type), @event.Type);
                //todo: determine how to write a byte array here.  can this be a json text at the end?
                writer.WriteBase64String(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.Metadata)) ?? nameof(@event.Metadata), @event.Metadata);
                writer.WriteBase64String(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.Data)) ?? nameof(@event.Data), @event.Data);
                writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(@event.MsgId)) ?? nameof(@event.MsgId), @event.MsgId!.Value);

                break;
            default:
                throw new JsonException();
        }

        writer.WriteEndObject();
    }

    private static StreamCreated DeserializeStreamCreated(ref Utf8JsonReader reader, JsonSerializerOptions options) {
        // find the StreamId node
        StreamId StreamId = null;
        long Position = -1;
        Guid MsgId = Guid.Empty;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = options?.PropertyNamingPolicy?.ConvertName(reader.GetString() ?? string.Empty) ?? reader.GetString();
            if (string.IsNullOrEmpty(propertyName)) continue;

            reader.Read(); // now at the value.

            if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(StreamId)) ?? nameof(StreamId))) {
                StreamId = JsonSerializer.Deserialize<StreamId>(ref reader, options)!;
                if (StreamId is null) throw new JsonException();
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(MsgId)) ?? nameof(MsgId))) {
                MsgId = Guid.Parse(reader.GetString()!);
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(Position)) ?? nameof(Position))) {
                Position = reader.GetInt64();
            }
        }

        if (StreamId is null || Position < 0l || MsgId == Guid.Empty) throw new JsonException();

        return new StreamCreated(StreamId, Position) { MsgId = MsgId };
    }

    private static RecordedEvent DeserializeRecordedEvent(ref Utf8JsonReader reader, JsonSerializerOptions options) {
        StreamId? StreamId = null;
        var EventId = Guid.Empty;
        var Metadata = Array.Empty<byte>();
        var Data = Array.Empty<byte>();
        var Type = string.Empty;
        var MsgId = Guid.Empty;
        var Position = -1L;

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
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(Metadata)) ?? nameof(Metadata))) {
                Metadata = reader.GetBytesFromBase64();
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(Data)) ?? nameof(Data))) {
                Data = reader.GetBytesFromBase64();
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(Type)) ?? nameof(Type))) {
                Type = reader.GetString()!;
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(MsgId)) ?? nameof(MsgId))) {
                MsgId = Guid.Parse(reader.GetString()!);
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(Position)) ?? nameof(Position))) {
                Position = reader.GetInt64();
            }
        }

        if (StreamId is null || EventId == Guid.Empty || Position < 0) throw new JsonException();

        return new RecordedEvent(StreamId, EventId, Position, Type, Metadata, Data) { MsgId = MsgId };
    }

    enum StreamItemTypes {
        Unknown = 0,
        StreamCreated = 1,
        RecordedEvent = 2,
    }
}