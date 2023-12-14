namespace LvStreamStore.ApplicationToolkit.Tests {
    internal static class TestMessages {
        public record TestEvent1 : Event;
        public record TestEvent2 : Event;
        public record NotRegisteredEvent : Event;

        public record TestCommand1 : Command;

        public record NotRegisteredCommand : Command;
    }
}
