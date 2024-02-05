namespace LvStreamStore.Tests {
    using FakeItEasy;

    using LvStreamStore;
    using LvStreamStore.Messaging;
    using LvStreamStore.Serialization.Json;
    using LvStreamStore.Test;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class MemoryClientTestFixture : IClientTestFixture {
        private MemoryEventStream? _stream;
        private IEventStreamClient _client;
        private ILoggerFactory _loggerFactory = LoggerFactory.Create((builder) => {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Trace);
        });

        public EventStream Stream {
            get {
                if (_stream == null) {
                    var value = new MemoryEventStreamOptions();
                    var options = A.Fake<IOptions<MemoryEventStreamOptions>>();
                    A.CallTo(() => options.Value)
                        .Returns(value);
                    var serializerOptions = A.Fake<IOptions<JsonSerializationOptions>>();
                    A.CallTo(() => serializerOptions.Value)
                        .Returns(new JsonSerializationOptions());

                    _stream = new MemoryEventStream(_loggerFactory, options);
                    AsyncHelper.RunSync(() => _stream.StartAsync());
                }
                return _stream;
            }
        }

        public IEventStreamClient Client {
            get {
                _client ??= new EmbeddedEventStreamClient(Dispatcher, Stream);
                return _client;
            }
        }

        public AsyncDispatcher Dispatcher { get; } = new();

        public void Dispose() {
            Stream?.Dispose();
        }
    }
}
