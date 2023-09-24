namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

using FakeItEasy;

using Microsoft.Extensions.Options;

public class InMemoryLocalStoreClientAllStreamTests : LocalStoreClientAllStreamTests<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister {
        get {
            var options = new SingleTenantInMemoryPersisterOptions();
            var fake = A.Fake<IOptions<SingleTenantInMemoryPersisterOptions>>();
            A.CallTo(() => fake.Value)
                .Returns(options);

            return new SingleTenantInMemoryPersister(new MemoryDataFileManager(), fake);
        }
    }
}
