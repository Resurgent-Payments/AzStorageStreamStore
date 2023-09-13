namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

using FakeItEasy;

using Microsoft.Extensions.Options;

public class OnDiskLocalStoreClientAllStreamTests : LocalStoreClientAllStreamTests<SingleTenantOnDiskPersister> {
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