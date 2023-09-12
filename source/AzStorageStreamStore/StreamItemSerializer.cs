namespace AzStorageStreamStore;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StreamItemConverter : JsonConverter<StreamItem> {
    public override bool CanConvert(Type typeToConvert) =>
        typeof(StreamItem).IsAssignableFrom(typeToConvert);

    public override StreamItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, StreamItem value, JsonSerializerOptions options) {
        throw new NotImplementedException();
    }
}