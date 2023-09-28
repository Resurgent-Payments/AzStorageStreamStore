namespace AzStorageStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Options;

using Xunit;

public abstract class LocalStoreClientAllStreamTests<TPersister> : IAsyncDisposable where TPersister : IPersister {
    private readonly IStoreClient _storeClient;
    private readonly StreamId _loadedStreamId = new("tenant-id", "some-id");
    IOptions<LocalDiskFileManagerOptions> _options;
    const string AllStreamEventType = "all-stream-event-type";

    protected abstract TPersister Persister { get; }


    public LocalStoreClientAllStreamTests() {
        var options = new LocalDiskFileManagerOptions {
            BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            FileReadBlockSize = 1024,
        };
        _options = A.Fake<IOptions<LocalDiskFileManagerOptions>>();
        A.CallTo(() => _options.Value)
            .Returns(options);

        _storeClient = new LocalStoreClient(Persister);

        AsyncHelper.RunSync(async () => await _storeClient.InitializeAsync());
        var result = AsyncHelper.RunSync(async () => await _storeClient.AppendToStreamAsync(_loadedStreamId, ExpectedVersion.Any, new[] { new EventData(_loadedStreamId, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>()) }));
        Assert.True(result.Successful);

    }

    [Fact]
    public async Task Can_subscribe_to_all_stream() {
        var events = new List<RecordedEvent>();
        var _mres = new ManualResetEventSlim(false);

        await _storeClient.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres.Set();
        });
        var key = new StreamId("test", "stream");

        var result = await _storeClient.AppendToStreamAsync(
            new StreamId("test", "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);


        _mres.Wait(500);

        Assert.Single(events);
    }

    [Fact]
    public async Task Can_subscribe_to_all_stream_multiple_times() {
        var events = new List<RecordedEvent>();

        var _mres1 = new ManualResetEventSlim(false);
        var _mres2 = new ManualResetEventSlim(false);

        await _storeClient.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres1.Set();
        });
        await _storeClient.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres2.Set();
        });

        var key = new StreamId("test", "stream");

        var result = await _storeClient.AppendToStreamAsync(
            new StreamId("test", "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);


        _mres1.Wait(200);
        _mres2.Wait(200);

        Assert.Equal(2, events.Count);
    }

    [Fact]
    public async Task Can_remove_an_allstream_subscription_via_returned_idsposable() {
        var events = new List<RecordedEvent>();
        var _mres = new ManualResetEventSlim(false);

        var disposer = await _storeClient.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres.Set();
        });
        disposer.Dispose();

        var key = new StreamId("test", "stream");
        var result = await _storeClient.AppendToStreamAsync(
            new StreamId("test", "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);

        _mres.Wait(100);

        Assert.Empty(events);
    }

    [Fact]
    public async Task Disposing_an_allstream_subscription_does_not_dispose_all_subscriptions() {
        var events = new List<RecordedEvent>();

        var _mres1 = new ManualResetEventSlim(false);
        var _mres2 = new ManualResetEventSlim(false);

        await _storeClient.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres1.Set();
        });

        // create and immedialy dispose to loop the subscription/disposal pipeline.
        (await _storeClient.SubscribeToAllAsync(x => {
            events.Add(x);
            _mres2.Set();
        })).Dispose();

        var key = new StreamId("test", "stream");

        var result = await _storeClient.AppendToStreamAsync(
            new StreamId("test", "stream"),
            ExpectedVersion.Any,
            new[] { new EventData(key, Guid.NewGuid(), AllStreamEventType, Array.Empty<byte>()) }
        );
        Assert.True(result.Successful);


        _mres1.Wait(100);
        _mres2.Wait(100);

        Assert.Single(events);
    }

    public ValueTask DisposeAsync() {
        Persister?.Dispose();
        return ValueTask.CompletedTask;
    }
}