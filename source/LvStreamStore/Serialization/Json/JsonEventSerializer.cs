namespace LvStreamStore.Serialization.Json {
    using System;
    using System.IO;
    using System.Text.Json;

    using Microsoft.Extensions.Options;

    public class JsonEventSerializer : IEventSerializer {
        JsonSerializationOptions _options;
        public JsonEventSerializer(IOptions<JsonSerializationOptions> options) {
            _options = options.Value ?? new JsonSerializationOptions();
        }

        public T Deserialize<T>(Stream stream) {
            return JsonSerializer.Deserialize<T>(stream, _options.JsonOptions)!;
        }

        public T Deserialize<T>(Span<byte> bytes) {
            return JsonSerializer.Deserialize<T>(bytes, _options.JsonOptions)!;
        }

        public Stream Serialize<T>(T @event) {
            var ms = new MemoryStream();
            JsonSerializer.Serialize(ms, @event, _options.JsonOptions);
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }
    }
}
