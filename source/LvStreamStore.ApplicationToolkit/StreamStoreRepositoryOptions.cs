namespace LvStreamStore.ApplicationToolkit {
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class StreamStoreRepositoryOptions {
        public JsonSerializerOptions JsonOptions { get; }

        public IList<JsonConverter> JsonConverters { get; set; } = new List<JsonConverter>();

        public StreamStoreRepositoryOptions() {
            JsonOptions = new JsonSerializerOptions() {
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    new JsonStringEnumConverter(),
                    new StreamItemJsonConverter(),
                    new DateOnlyJsonConverter(),
                    new TimeOnlyJsonConverter()
                }
            };
            foreach (var converter in JsonConverters) {
                JsonOptions.Converters.Add(converter);
            }
        }
    }
}
