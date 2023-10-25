namespace LvStreamStore.Serialization.Json {
    using System.Text.Json;
    using System.Text.Json.Serialization;

    public class JsonSerializationOptions {
        public JsonSerializerOptions JsonOptions { get; }

        internal JsonSerializationOptions() {
            JsonOptions = new JsonSerializerOptions() {
                IgnoreReadOnlyFields = false,
                IgnoreReadOnlyProperties = false,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    new JsonStringEnumConverter(),
                    new StreamItemJsonConverter()
                }
            };
        }
    }
}
