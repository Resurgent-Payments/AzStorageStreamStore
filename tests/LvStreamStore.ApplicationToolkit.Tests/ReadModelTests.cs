namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;

    using LvStreamStore.Test;

    using Xunit;

    public partial class ReadModelTests : StreamStoreTestSpecification {
        [Fact]
        public void CanSubscribeToAggregateEvents() {
            var readModel = new TestReadModel(Dispatcher, Repository);
            Repository.Save(new TestAggregate(Guid.NewGuid(), "name", "description"));
            AssertEx.IsOrBecomesTrue(() => readModel.UIModels.Count > 0, TimeSpan.FromSeconds(3));
        }
    }
}
