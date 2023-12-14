namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;

    internal class TestAggregate : AggregateRoot {
        string _name = string.Empty;
        string _description = string.Empty;

        public TestAggregate(Guid testAggregateId, string name, string description) {
            RegisterEvents();
            Raise(new TestAggregateMsgs.Created(testAggregateId, name, description));
        }

        public TestAggregate() {
            RegisterEvents();
        }

        private void RegisterEvents() {
            Register<TestAggregateMsgs.Created>(Apply);
            Register<TestAggregateMsgs.NameChanged>(Apply);
            Register<TestAggregateMsgs.DescriptionChanged>(Apply);
        }

        public void ChangeName(string name) {
            if (_name.Equals(name)) return;
            Raise(new TestAggregateMsgs.NameChanged(Id, name));
        }

        public void ChangeDescription(string description) {
            if (_description.Equals(description)) return;
            Raise(new TestAggregateMsgs.DescriptionChanged(Id, description));
        }

        private void Apply(TestAggregateMsgs.Created msg) {
            Id = msg.TestAggregateId;
            _name = msg.Name;
            _description = msg.Description;
        }

        private void Apply(TestAggregateMsgs.NameChanged msg) {
            _name = msg.Name;
        }

        private void Apply(TestAggregateMsgs.DescriptionChanged msg) {
            _description = msg.Description;
        }
    }
}
