namespace AzStorageStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Options;

using Xunit;

[Trait("Type", "Integration")]
public class InMemoryStreamClientIntegrationTests : StreamClientIntegrationTestBase<SingleTenantPersister> {
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

[Trait("Type", "Integration")]
public class LocalDiskPersisterIntegrationTests : StreamClientIntegrationTestBase<SingleTenantPersister> {
    protected override SingleTenantPersister Persister {
        get {
            var diskOptions = new LocalDiskFileManagerOptions {
                BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                FileReadBlockSize = 4096 // 4k block size.
            };
            var diskOptionsAccessor = A.Fake<IOptions<LocalDiskFileManagerOptions>>();
            A.CallTo(() => diskOptionsAccessor.Value)
                .Returns(diskOptions);

            var persisterOptions = new SingleTenantPersisterOptions();
            var persisterOptionsAccessor = A.Fake<IOptions<SingleTenantPersisterOptions>>();
            A.CallTo(() => persisterOptionsAccessor.Value)
                .Returns(persisterOptions);


            return new SingleTenantPersister(new LocalDiskFileManager(diskOptionsAccessor), persisterOptionsAccessor);
        }
    }
}