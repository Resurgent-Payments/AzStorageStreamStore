namespace LvStreamStore;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public abstract class EventStreamOptions {
    public JsonSerializerOptions JsonOptions { get; }
    public IList<JsonConverter> JsonConverters { get; }

    public EventStreamOptions() {
        JsonConverters = new List<JsonConverter>();
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
        foreach (var converter in JsonConverters) {
            JsonOptions.Converters.Add(converter);
        }
    }
}