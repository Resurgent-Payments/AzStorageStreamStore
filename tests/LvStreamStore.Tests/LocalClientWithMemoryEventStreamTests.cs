namespace LvStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

public class LocalClientWithMemoryEventStreamTests : ClientTestBase {
    private MemoryEventStream? _stream;

    protected override EventStream Stream {
        get {
            if (_stream == null) {
                var value = new MemoryEventStreamOptions();
                var options = A.Fake<IOptions<EventStreamOptions>>();
                A.CallTo(() => options.Value)
                    .Returns(value);

                _stream = new MemoryEventStream(NullLoggerFactory.Instance, options);
            }
            return _stream;
        }
    }
}
