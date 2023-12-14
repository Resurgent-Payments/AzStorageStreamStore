namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    }
}
