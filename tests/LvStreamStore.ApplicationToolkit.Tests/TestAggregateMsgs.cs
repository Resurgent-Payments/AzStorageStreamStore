namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;

    internal class TestAggregateMsgs {
        public record Created(Guid TestAggregateId, string Name, string Description) : Event;
        public record NameChanged(Guid TestAggreateId, string Name) : Event;
        public record DescriptionChanged(Guid TestAggregateId, string Description) : Event;
    }
}
