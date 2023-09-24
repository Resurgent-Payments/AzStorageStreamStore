namespace AzStorageStreamStore {
    using System.Text.Json;
    using System.Text.Json.Serialization;

    internal class JsonSerializationConstants {
        static JsonSerializationConstants() {
            Options = new JsonSerializerOptions() {
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IncludeFields = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = {
                    new JsonStringEnumConverter(),
                    new StreamIdJsonConverter(),
                    new StreamItemJsonConverter()
                }
            };
        }

        public static JsonSerializerOptions Options { get; private set; }
    }
}
