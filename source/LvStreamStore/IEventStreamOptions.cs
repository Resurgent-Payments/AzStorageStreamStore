namespace LvStreamStore;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public interface IEventStreamOptions {
    JsonSerializerOptions JsonOptions { get; }
    IList<JsonConverter> JsonConverters { get; set; }
}