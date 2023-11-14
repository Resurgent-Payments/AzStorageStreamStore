namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;

    public partial class ReadModelTests {
        class TestAggregateMsgs {
            public record Created(Guid TestAggregateId, string Name, string Description, Guid? MsgId = null) : Event(MsgId);
            public record NameChanged(Guid TestAggreateId, string Name, Guid? MsgId = null) : Event(MsgId);
            public record DescriptionChanged(Guid TestAggregateId, string Description, Guid? MsgId = null) : Event(MsgId);
        }
    }
}
