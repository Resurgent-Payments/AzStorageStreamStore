namespace LvStreamStore.ApplicationToolkit.Tests {
    using LvStreamStore.Messaging;

    internal static class TestMessages {
        public record TestEvent1 : Event;
        public record TestEvent2 : Event;
        public record NotRegisteredEvent : Event;

        public record TestCommand1 : Message;

        public record NotRegisteredCommand : Message;
    }
}
