namespace LvStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Logging;
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
                var options = A.Fake<IOptions<EventStreamOptions>>();
                A.CallTo(() => options.Value)
                    .Returns(value);

                _stream = new MemoryEventStream(_loggerFactory, options);
            }
            return _stream;
        }
    }
}
