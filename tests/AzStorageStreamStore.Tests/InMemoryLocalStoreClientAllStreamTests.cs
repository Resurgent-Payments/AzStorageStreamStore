namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

using FakeItEasy;

using Microsoft.Extensions.Options;

public class InMemoryLocalStoreClientAllStreamTests : LocalStoreClientAllStreamTests<SingleTenantPersister> {
    protected override SingleTenantPersister Persister {
        get {
            var options = new SingleTenantPersisterOptions();
            var fake = A.Fake<IOptions<SingleTenantPersisterOptions>>();
            A.CallTo(() => fake.Value)
                .Returns(options);

            return new SingleTenantPersister(new MemoryDataFileManager(), fake);
        }
    }
}
