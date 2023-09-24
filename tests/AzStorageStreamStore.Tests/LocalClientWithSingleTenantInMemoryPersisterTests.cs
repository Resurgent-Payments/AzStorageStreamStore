namespace AzStorageStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Options;

public class LocalClientWithSingleTenantInMemoryPersisterTests : ClientTestBase<SingleTenantPersister> {
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
