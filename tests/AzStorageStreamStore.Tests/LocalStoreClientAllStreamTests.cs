//namespace AzStorageStreamStore.Tests;

//using FakeItEasy;

//using Microsoft.Extensions.Options;

//using Xunit;

//public abstract class LocalStoreClientAllStreamTests : IAsyncDisposable {
//    private readonly IStoreClient _storeClient;
//    private readonly StreamId _loadedStreamId = new("tenant-id", "some-id");
//    IOptions<LocalDiskFileManagerOptions> _options;
//    const string AllStreamEventType = "all-stream-event-type";

//    protected abstract EventStream Stream { get; }


//    public LocalStoreClientAllStreamTests() {
//        var options = new LocalDiskFileManagerOptions {
//            BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
//            FileReadBlockSize = 1024,
//        };
//        _options = A.Fake<IOptions<LocalDiskFileManagerOptions>>();
//        A.CallTo(() => _options.Value)
//            .Returns(options);

//        _storeClient = new LocalStoreClient(Persister);

//        AsyncHelper.RunSync(async () => await _storeClient.InitializeAsync());
//        var result = AsyncHelper.RunSync(async () => await _storeClient.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { new EventData(_loadedStreamId, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>(), Array.Empty<byte>()) }));
//        Assert.True(result.Successful);

//    }

//    public ValueTask DisposeAsync() {
//        Persister?.Dispose();
//        return ValueTask.CompletedTask;
//    }
//}