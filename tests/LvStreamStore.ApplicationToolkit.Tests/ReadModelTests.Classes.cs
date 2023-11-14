namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public partial class ReadModelTests {

        class TestReadModel : ReadModelBase,
            IAsyncHandler<TestAggregateMsgs.Created>,
            IAsyncHandler<TestAggregateMsgs.NameChanged>,
            IAsyncHandler<TestAggregateMsgs.DescriptionChanged> {

            private IDictionary<Guid, UIModel> _uiModels = new Dictionary<Guid, UIModel>();

            public IReadOnlyCollection<UIModel> UIModels => _uiModels.Values.ToList().AsReadOnly();

            public TestReadModel(ISubscriber inBus, IStreamStoreRepository repository) : base(inBus, repository) {
                SubscribeToStream<TestAggregate, TestAggregateMsgs.Created>(this);
                SubscribeToStream<TestAggregate, TestAggregateMsgs.NameChanged>(this);
                SubscribeToStream<TestAggregate, TestAggregateMsgs.DescriptionChanged>(this);
            }

            public ValueTask HandleAsync(TestAggregateMsgs.Created @event) {
                _uiModels.Add(@event.TestAggregateId, new UIModel(@event.TestAggregateId) { Name = @event.Name, Description = @event.Description });
                return ValueTask.CompletedTask;
            }

            public ValueTask HandleAsync(TestAggregateMsgs.NameChanged @event) {
                if (_uiModels.TryGetValue(@event.TestAggreateId, out var model)) {
                    model.Name = @event.Name;
                }
                return ValueTask.CompletedTask;
            }

            public ValueTask HandleAsync(TestAggregateMsgs.DescriptionChanged @event) {
                if (_uiModels.TryGetValue(@event.TestAggregateId, out var model)) {
                    model.Description = @event.Description;
                }
                return ValueTask.CompletedTask;
            }

            public class UIModel {
                public readonly Guid TestAggregateId;
                public string Name { get; set; } = string.Empty;
                public string Description { get; set; } = string.Empty;

                public UIModel(Guid testAggregateId) {
                    TestAggregateId = testAggregateId;
                }
            }
        }

        class TestAggregate : AggregateRoot {
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

        class TestAggregateMsgs {
            public record Created(Guid TestAggregateId, string Name, string Description, Guid? MsgId = null) : Event(MsgId);
            public record NameChanged(Guid TestAggreateId, string Name, Guid? MsgId = null) : Event(MsgId);
            public record DescriptionChanged(Guid TestAggregateId, string Description, Guid? MsgId = null) : Event(MsgId);
        }
    }
}
