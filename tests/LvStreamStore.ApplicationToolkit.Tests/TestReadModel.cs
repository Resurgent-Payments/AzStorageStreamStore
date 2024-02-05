namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using LvStreamStore.Messaging;

    public partial class ReadModelTests {
        class TestReadModel : ReadModelBase,
            IReceiver<TestAggregateMsgs.Created>,
            IReceiver<TestAggregateMsgs.NameChanged>,
            IReceiver<TestAggregateMsgs.DescriptionChanged> {

            private IDictionary<Guid, UIModel> _uiModels = new Dictionary<Guid, UIModel>();

            public IReadOnlyCollection<UIModel> UIModels => _uiModels.Values.ToList().AsReadOnly();

            public TestReadModel(AsyncDispatcher dispatcher, IStreamStoreRepository repository) : base(dispatcher, repository) {
                SubscribeToStream<TestAggregateMsgs.Created>(this);
                SubscribeToStream<TestAggregateMsgs.NameChanged>(this);
                SubscribeToStream<TestAggregateMsgs.DescriptionChanged>(this);
            }

            public Task Receive(TestAggregateMsgs.Created @event) {
                _uiModels.Add(@event.TestAggregateId, new UIModel(@event.TestAggregateId) { Name = @event.Name, Description = @event.Description });
                return Task.CompletedTask;
            }

            public Task Receive(TestAggregateMsgs.NameChanged @event) {
                if (_uiModels.TryGetValue(@event.TestAggreateId, out var model)) {
                    model.Name = @event.Name;
                }
                return Task.CompletedTask;
            }

            public Task Receive(TestAggregateMsgs.DescriptionChanged @event) {
                if (_uiModels.TryGetValue(@event.TestAggregateId, out var model)) {
                    model.Description = @event.Description;
                }
                return Task.CompletedTask;
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
