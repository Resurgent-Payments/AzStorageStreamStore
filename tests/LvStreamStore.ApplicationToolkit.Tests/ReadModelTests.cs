namespace LvStreamStore.ApplicationToolkit.Tests {
    using System;

    using LvStreamStore.Test;

    using Xunit;

    public partial class ReadModelTests : StreamStoreTestSpecification {
        private readonly TestReadModel _readModel;

        public ReadModelTests() : base() {
            _readModel = new TestReadModel(Bus, Repository);
        }

        [Fact]
        public void CanSubscribeToAggregateEvents() {
            Repository.Save(new TestAggregate(Guid.NewGuid(), "name", "description"));
            AssertEx.IsOrBecomesTrue(() => _readModel.UIModels.Count > 0, TimeSpan.FromSeconds(3));
        }
    }
}
