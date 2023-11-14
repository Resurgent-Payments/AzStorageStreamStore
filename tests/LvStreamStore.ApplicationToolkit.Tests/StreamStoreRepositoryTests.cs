namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using FakeItEasy;

    using LvStreamStore.Serialization.Json;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;

    using Xunit;

    public class StreamStoreRepositoryTests : IAsyncLifetime {
        ILoggerFactory _loggerFactory;
        IStreamStoreRepository _repository;

        public StreamStoreRepositoryTests() {
            _loggerFactory = LoggerFactory.Create((builder) => {
                builder.AddDebug();
                builder.SetMinimumLevel(LogLevel.Trace);
            });

            var jsSerOptions = new JsonSerializationOptions();
            var jsOpts = A.Fake<IOptions<JsonSerializationOptions>>();
            A.CallTo(() => jsOpts.Value).Returns(jsSerOptions);
            var jsonEventSerializer = new JsonEventSerializer(jsOpts);

            var msStreamOpts = new MemoryEventStreamOptions();
            var msOpts = A.Fake<IOptions<MemoryEventStreamOptions>>();
            A.CallTo(() => msOpts.Value).Returns(msStreamOpts);

            var ssRepOpts = new StreamStoreRepositoryOptions();
            var ssOpts = A.Fake<IOptions<StreamStoreRepositoryOptions>>();
            A.CallTo(() => ssOpts.Value).Returns(ssRepOpts);

            var eventStream = new MemoryEventStream(_loggerFactory, jsonEventSerializer, msOpts);
            var client = new EmbeddedEventStreamClient(eventStream);
            _repository = new StreamStoreRepository(client, ssOpts);
        }

        public async Task InitializeAsync() {
            var agg = new TestAggregate(Guid.NewGuid(), "name", "description");
            await _repository.Save(agg);
        }

        [Fact]
        public async Task ARepositoryCanBeRead() {
            var reader = _repository.ReadAsync(StreamKey.All);
            Assert.Single(await reader.ToListAsync());
        }

        public Task DisposeAsync() => Task.CompletedTask;
    }
}
