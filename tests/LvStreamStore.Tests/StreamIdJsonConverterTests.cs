namespace LvStreamStore.Tests {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Xunit;

    public class StreamIdJsonConverterTests {
        MemoryEventStreamOptions _options = new();
        StreamIdJsonConverter _sut = new StreamIdJsonConverter();

        [Fact]
        public void An_id_can_be_serialized_and_rehydrated() {
            var id = new StreamId("gift", new[] { "core", "transaction" }, "22062505-F799-44A6-BCEC-76BAF9EB0D28");


        }
    }
}
