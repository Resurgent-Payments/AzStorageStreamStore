namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;

    using FakeItEasy;

    using LvStreamStore.Serialization.Json;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Xunit;

    public partial class ReadModelTests {
        private readonly MemoryEventStream _eventStream;
        private readonly ILoggerFactory _loggerFactory;
        private readonly Dispatcher _bus;
        private readonly IStreamStoreRepository _repository;
        private readonly TestReadModel _readModel;

        public ReadModelTests() {
            _loggerFactory = LoggerFactory.Create((builder) => {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Trace);
            });
            _bus = new Dispatcher(_loggerFactory);

            var msOptions = new MemoryEventStreamOptions();
            var serOptions = new JsonSerializationOptions();
            var ssRepOptions = new StreamStoreRepositoryOptions();

            var iOptsMsOpts = A.Fake<IOptions<MemoryEventStreamOptions>>();
            var serOpts = A.Fake<IOptions<JsonSerializationOptions>>();
            var ssRepOpts = A.Fake<IOptions<StreamStoreRepositoryOptions>>();

            A.CallTo(() => iOptsMsOpts.Value).Returns(msOptions);
            A.CallTo(() => serOpts.Value).Returns(serOptions);
            A.CallTo(() => ssRepOpts.Value).Returns(ssRepOptions);

            _eventStream = new MemoryEventStream(_loggerFactory, new JsonEventSerializer(serOpts), iOptsMsOpts);
            _repository = new StreamStoreRepository(new EmbeddedEventStreamClient(_eventStream), ssRepOpts);
            _readModel = new TestReadModel(_bus, _repository);
        }

        [Fact]
        public void CanSubscribeToAggregateEvents() {
            _repository.Save(new TestAggregate(Guid.NewGuid(), "name", "description"));
            AssertEx.IsOrBecomesTrue(() => _readModel.UIModels.Count > 0, TimeSpan.FromSeconds(3));
        }
    }
}
