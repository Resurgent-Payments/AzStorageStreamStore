namespace AzStorageStreamStore;

using System.Text.Json;
using System.Text.Json.Serialization;

public class SingleTenantOnDiskPersisterOptions {
    public string BaseDataPath { get; set; }
    public int FileReadBlockSize { get; set; } = 4096;
    public JsonSerializerOptions JsonOptions { get; private set; }
    public IList<JsonConverter> JsonConverters { get; set; } = new List<JsonConverter>();

    public SingleTenantOnDiskPersisterOptions() {
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