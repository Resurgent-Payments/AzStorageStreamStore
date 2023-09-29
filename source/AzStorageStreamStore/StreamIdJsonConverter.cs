namespace AzStorageStreamStore;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StreamIdJsonConverter : JsonConverter<StreamId> {
    public override StreamId? Read(ref Utf8JsonReader reader, Type _, JsonSerializerOptions options) {
        string Id = options?.PropertyNamingPolicy?.ConvertName(nameof(Id)) ?? nameof(Id);
        string TenantId = options?.PropertyNamingPolicy?.ConvertName(nameof(TenantId)) ?? nameof(TenantId);

        var idProperty = string.Empty;
        var tenantIdProperty = string.Empty;


        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject) {
            var propertyName = reader.GetString();

            if (string.IsNullOrEmpty(propertyName)) continue;

            reader.Read(); // now at the value.

            if (propertyName.Equals(Id)) {
                idProperty = reader.GetString();
            } else if (propertyName.Equals(TenantId)) {
                tenantIdProperty = reader.GetString();
            }
        }

        return (!string.IsNullOrEmpty(idProperty) && !string.IsNullOrEmpty(tenantIdProperty))
            ? new StreamId(tenantIdProperty, Array.Empty<string>(), idProperty) // todo: de-serialize array.
            : null;
    }

    public override void Write(Utf8JsonWriter writer, StreamId value, JsonSerializerOptions options) {
        writer.WriteStartObject();

        writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(value.Id)) ?? nameof(value.Id), value.Id);
        writer.WriteStartArray(options?.PropertyNamingPolicy?.ConvertName(nameof(value.Hierarchy)) ?? nameof(value.Hierarchy));
        foreach (var item in value.Hierarchy) { writer.WriteStringValue(item); }
        writer.WriteEndArray();
        writer.WriteString(options?.PropertyNamingPolicy?.ConvertName(nameof(value.TenantId)) ?? nameof(value.TenantId), value.TenantId);

        writer.WriteEndObject();
    }
}