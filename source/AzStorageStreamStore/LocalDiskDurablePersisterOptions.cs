namespace AzStorageStreamStore;

using System.Text.Json;
using System.Text.Json.Serialization;

public class LocalDiskDurablePersisterOptions {
    public string BaseDataPath { get; set; }
    public int FileReadBlockSize { get; set; } = 4096;
    public JsonSerializerOptions JsonOptions { get; set; }
    public IList<JsonConverter> JsonConverters { get; set; } = new List<JsonConverter>();

    public LocalDiskDurablePersisterOptions() {
        JsonOptions = new JsonSerializerOptions() {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            Converters = {
                new JsonStringEnumConverter()
            }
        };
        foreach (var converter in JsonConverters) {
            JsonOptions.Converters.Add(converter);
        }
    }
}