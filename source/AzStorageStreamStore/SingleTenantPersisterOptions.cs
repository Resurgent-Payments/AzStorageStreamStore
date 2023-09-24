namespace AzStorageStreamStore;

using System.Text.Json.Serialization;
using System.Text.Json;

public class SingleTenantPersisterOptions {
    public JsonSerializerOptions JsonOptions { get; private set; }
    public IList<JsonConverter> JsonConverters { get; set; } = new List<JsonConverter>();

    public SingleTenantPersisterOptions() {
        JsonOptions = new JsonSerializerOptions() {
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
        foreach (var converter in JsonConverters) {
            JsonOptions.Converters.Add(converter);
        }
    }
}