namespace LvStreamStore;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

internal class StreamIdJsonConverter : JsonConverter<StreamId> {
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAssignableFrom(typeof(StreamId));

    public override StreamId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        var cloned = reader;

        if (cloned.TokenType != JsonTokenType.StartObject) throw new JsonException();

        var TenantId = string.Empty;
        var Hierarchy = new List<string>();
        var ObjectId = string.Empty;

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = options?.PropertyNamingPolicy?.ConvertName(reader.GetString() ?? string.Empty) ?? reader.GetString();
            if (string.IsNullOrEmpty(propertyName)) continue;

            reader.Read(); // now at the value.

            if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(TenantId)) ?? nameof(TenantId))) {
                TenantId = reader.GetString()!;
            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(TenantId)) ?? nameof(TenantId))) {

                reader.Read();

                while (reader.TokenType != JsonTokenType.EndArray) {
                    Hierarchy.Add(reader.GetString() ?? string.Empty);
                }

                reader.Read();

            } else if (propertyName.Equals(options?.PropertyNamingPolicy?.ConvertName(nameof(ObjectId)) ?? nameof(ObjectId))) {
                ObjectId = reader.GetString()!;
            }
        }

        return new StreamId(TenantId, Hierarchy.ToArray(), ObjectId);
    }

    public override void Write(Utf8JsonWriter writer, StreamId value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(value.TenantId)) ?? nameof(value.TenantId), value.TenantId);

        writer.WriteStartArray(options?.PropertyNamingPolicy?.ConvertName(nameof(value.Hierarchy)) ?? nameof(value.Hierarchy));

        foreach (var item in value.Hierarchy) {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();

        writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(value.ObjectId)) ?? nameof(value.ObjectId), value.ObjectId);

        writer.WriteEndObject();
    }
}