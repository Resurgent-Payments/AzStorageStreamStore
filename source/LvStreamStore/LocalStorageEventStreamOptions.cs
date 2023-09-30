namespace LvStreamStore;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class LocalStorageEventStreamOptions : IEventStreamOptions {
    public JsonSerializerOptions JsonOptions { get; private set; }
    public IList<JsonConverter> JsonConverters { get; set; } = new List<JsonConverter>();
    public string BaseDataPath { get; set; } = string.Empty;
    public int FileReadBlockSize { get; set; } = 4096;

    public LocalStorageEventStreamOptions() {
        JsonOptions = new JsonSerializerOptions() {
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = {
                new JsonStringEnumConverter(),
                //new StreamIdJsonConverter(),
                new StreamItemJsonConverter()
            }
        };
        foreach (var converter in JsonConverters) {
            JsonOptions.Converters.Add(converter);
        }
    }
}
