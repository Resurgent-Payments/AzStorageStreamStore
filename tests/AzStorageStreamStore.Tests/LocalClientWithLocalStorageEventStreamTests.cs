namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

using FakeItEasy;

using Microsoft.Extensions.Options;

public class LocalClientWithLocalStorageEventStreamTests : ClientTestBase {
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
