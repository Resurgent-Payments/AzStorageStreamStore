namespace LvStreamStore.Test {
    using FakeItEasy;

    using LvStreamStore.ApplicationToolkit;
    using LvStreamStore.Messaging;
    using LvStreamStore.Serialization;
    using LvStreamStore.Serialization.Json;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    public class StreamStoreTestSpecification {
        public ILoggerFactory LoggingFactory { get; }
        public EventStream Stream { get; }
        public IEventSerializer EventSerializer { get; }
        public IStreamStoreRepository Repository { get; }
        public IEventStreamClient StreamClient { get; }
        public AsyncDispatcher Dispatcher { get; }

        public StreamStoreTestSpecification() {
            var jsonSerializationConfiguration = new JsonSerializationOptions();
            var jsonSerializationOptions = A.Fake<IOptions<JsonSerializationOptions>>();
            A.CallTo(() => jsonSerializationOptions.Value).Returns(jsonSerializationConfiguration);

            var repositoryConfiguration = new StreamStoreRepositoryOptions();
            var repositoryOptions = A.Fake<IOptions<StreamStoreRepositoryOptions>>();
            A.CallTo(() => repositoryOptions.Value).Returns(repositoryConfiguration);

            var eventStreamConfiguration = new MemoryEventStreamOptions();
            var eventStreamOptions = A.Fake<IOptions<MemoryEventStreamOptions>>();
            A.CallTo(() => eventStreamOptions.Value).Returns(eventStreamConfiguration);

            LoggingFactory = LoggerFactory.Create(builder => {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            EventSerializer = new JsonEventSerializer(jsonSerializationOptions);
            Stream = new MemoryEventStream(LoggingFactory, eventStreamOptions);
            Dispatcher = new AsyncDispatcher(LoggingFactory);
            StreamClient = new EmbeddedEventStreamClient(Dispatcher, Stream);
            Repository = new StreamStoreRepository(StreamClient, repositoryOptions);

            AsyncHelper.RunSync(() => Stream.StartAsync());
        }
    }
}
