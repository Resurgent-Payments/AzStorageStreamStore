namespace LvStreamStore.LocalStorage.Tests {
    using System;
    using System.Threading.Tasks;

    using FakeItEasy;

    using LvStreamStore;
    using LvStreamStore.Messaging;
    using LvStreamStore.Serialization.Json;
    using LvStreamStore.Test;
    using LvStreamStore.Tests;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class LocalStorageTestFixtureWithoutCaching : IClientTestFixture {
        IEventStreamClient? _client;
        private EventStream? _stream;
        private ILoggerFactory _loggerFactory = LoggerFactory.Create((builder) => {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Trace);
        });
        private LocalStorageEventStreamOptions? _diskOptions;

        public EventStream Stream {
            get {
                if (_stream == null) {
                    _diskOptions = new LocalStorageEventStreamOptions {
                        BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                        FileReadBlockSize = 1_048_576, // 1mb. // 4096, // 4k block size.
                        UseCaching = false,
                    };
                    var diskOptionsAccessor = A.Fake<IOptions<LocalStorageEventStreamOptions>>();
                    A.CallTo(() => diskOptionsAccessor.Value)
                        .Returns(_diskOptions);
                    var serializerOptions = A.Fake<IOptions<JsonSerializationOptions>>();
                    A.CallTo(() => serializerOptions.Value)
                        .Returns(new JsonSerializationOptions());

                    _stream = new LocalStorageEventStream(_loggerFactory, new JsonEventSerializer(serializerOptions), diskOptionsAccessor);
                    AsyncHelper.RunSync(() => _stream.StartAsync());
                }
                return _stream;
            }
        }

        public IEventStreamClient Client {
            get {
                _client ??= new EmbeddedEventStreamClient(Dispatcher, Stream, _loggerFactory);
                return _client;
            }
        }

        public AsyncDispatcher Dispatcher { get; } = new();

        public async Task InitializeAsync() {
            await Client.Connect();
        }

        public Task DisposeAsync() {
            Client?.Dispose();
            Stream?.Dispose();
            Directory.Delete(_diskOptions!.BaseDataPath, true);
            return Task.CompletedTask;
        }

    }
}
