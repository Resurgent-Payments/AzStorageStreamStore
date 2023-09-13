namespace AzStorageStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Options;

public class InMemoryPersisterTests : StoreClientTestBase<InMemoryPersister> {
    protected override InMemoryPersister Persister => new InMemoryPersister();
}

public class LocalDiskPersisterTests : StoreClientTestBase<SingleTenantOnDiskPersister> {

    protected override SingleTenantOnDiskPersister Persister {
        get {
            var options = new SingleTenantOnDiskPersisterOptions {
                BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                FileReadBlockSize = 1024,
            };
            var fake = A.Fake<IOptions<SingleTenantOnDiskPersisterOptions>>();
            A.CallTo(() => fake.Value)
                .Returns(options);

            return new SingleTenantOnDiskPersister(fake);
        }
    }
}

public class InMemorySingleTenantStoreClientTests : SingleTenantStoreClientTests<InMemoryPersister> {
    protected override InMemoryPersister Persister => new InMemoryPersister();
}

public class LocalDiskSingleTenantStoreClientTests : SingleTenantStoreClientTests<SingleTenantOnDiskPersister> {
    protected override SingleTenantOnDiskPersister Persister {
        get {
            var options = new SingleTenantOnDiskPersisterOptions {
                BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                FileReadBlockSize = 1024,
            };
            var fake = A.Fake<IOptions<SingleTenantOnDiskPersisterOptions>>();
            A.CallTo(() => fake.Value)
                .Returns(options);

            return new SingleTenantOnDiskPersister(fake);
        }
    }
}