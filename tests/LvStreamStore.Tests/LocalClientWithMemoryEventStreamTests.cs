namespace LvStreamStore.Tests;

using FakeItEasy;

using LvStreamStore.Serialization.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class LocalClientWithMemoryEventStreamTests : ClientTestBase {
    private MemoryEventStream? _stream;
    private ILoggerFactory _loggerFactory = LoggerFactory.Create((builder) => {
        builder.AddDebug();
        builder.SetMinimumLevel(LogLevel.Trace);
    });

    protected override EventStream Stream {
        get {
            if (_stream == null) {
                var value = new MemoryEventStreamOptions();
                var options = A.Fake<IOptions<MemoryEventStreamOptions>>();
                A.CallTo(() => options.Value)
                    .Returns(value);
                var serializerOptions = A.Fake<IOptions<JsonSerializationOptions>>();
                A.CallTo(() => serializerOptions.Value)
                    .Returns(new JsonSerializationOptions());



                _stream = new MemoryEventStream(_loggerFactory, new JsonEventSerializer(serializerOptions), options);
            }
            return _stream;
        }
    }
}
