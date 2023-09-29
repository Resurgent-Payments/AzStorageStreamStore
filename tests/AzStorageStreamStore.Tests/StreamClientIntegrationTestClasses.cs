namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

using FakeItEasy;

using Microsoft.Extensions.Options;

using Xunit;

[Trait("Type", "Integration")]
public class InMemoryStreamClientIntegrationTests : StreamClientIntegrationTestBase {
    private MemoryEventStream? _stream;

    protected override EventStream Stream {
        get {
            if (_stream == null) {
                var value = new MemoryEventStreamOptions();
                var options = A.Fake<IOptions<IEventStreamOptions>>();
                A.CallTo(() => options.Value)
                    .Returns(value);

                _stream = new MemoryEventStream(options);
            }
            return _stream;
        }
    }
}

[Trait("Type", "Integration")]
public class LocalDiskPersisterIntegrationTests : StreamClientIntegrationTestBase {
    private EventStream? _stream;
    protected override EventStream Stream {
        get {
            if (_stream == null) {
                var diskOptions = new LocalStorageEventStreamOptions {
                    BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                    FileReadBlockSize = 4096 // 4k block size.
                };
                var diskOptionsAccessor = A.Fake<IOptions<IEventStreamOptions>>();
                A.CallTo(() => diskOptionsAccessor.Value)
                    .Returns(diskOptions);

                _stream = new LocalStorageEventStream(diskOptionsAccessor);
            }
            return _stream;
        }
    }
}