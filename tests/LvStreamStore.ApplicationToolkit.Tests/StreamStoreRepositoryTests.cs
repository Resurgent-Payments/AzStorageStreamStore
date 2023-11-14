//namespace LvStreamStore.ApplicationToolkit.Tests {
//    using System;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.Text;
//    using System.Threading.Tasks;

//    using FakeItEasy;

//    using LvStreamStore.Serialization.Json;

//    using Microsoft.Extensions.Logging;
//    using Microsoft.Extensions.Options;

//    using Xunit;

//    public class StreamStoreRepositoryTests {
//        ILoggerFactory _loggerFactory;
//        IStreamStoreRepository _repository;

//        public StreamStoreRepositoryTests() {
//            var jsSerOptions = new JsonSerializationOptions();
//            var jsOpts = A.Fake<IOptions<JsonSerializationOptions>>();
//            A.CallTo(() => jsOpts.Value).Returns(jsSerOptions);
//            var jsonEventSerializer = new JsonEventSerializer(jsOpts);

//            var msStreamOpts = new MemoryEventStreamOptions();
//            var msOpts = A.Fake<IOptions<MemoryEventStreamOptions>>();
//            A.CallTo(() => msOpts.Value).Returns(msStreamOpts);

//            var ssRepOpts = new StreamStoreRepositoryOptions();
//            var ssOpts = A.Fake<IOptions<StreamStoreRepositoryOptions>>();
//            A.CallTo(() => ssOpts.Value).Returns(ssRepOpts);

//            var eventStream = new MemoryEventStream(_loggerFactory, jsonEventSerializer, msOpts);
//            var client = new EmbeddedEventStreamClient(eventStream);
//            _repository = new StreamStoreRepository(client, ssOpts);
//        }

//        [Fact]
//        public void ARepositoryCanBeRead() {
//            _repository.ReadAsync()
//        }
//    }
//}
