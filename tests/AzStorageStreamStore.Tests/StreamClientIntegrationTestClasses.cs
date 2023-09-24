namespace AzStorageStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Options;

using Xunit;

[Trait("Type", "Integration")]
public class InMemoryStreamClientIntegrationTests : StreamClientIntegrationTestBase<SingleTenantInMemoryPersister> {
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

[Trait("Type", "Integration")]
public class LocalDiskPersisterIntegrationTests : StreamClientIntegrationTestBase<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister {
        get {
            var diskOptions = new LocalDiskFileManagerOptions {
                BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                FileReadBlockSize = 4096 // 4k block size.
            };
            var diskOptionsAccessor = A.Fake<IOptions<LocalDiskFileManagerOptions>>();
            A.CallTo(() => diskOptionsAccessor.Value)
                .Returns(diskOptions);

            var persisterOptions = new SingleTenantInMemoryPersisterOptions();
            var persisterOptionsAccessor = A.Fake<IOptions<SingleTenantInMemoryPersisterOptions>>();
            A.CallTo(() => persisterOptionsAccessor.Value)
                .Returns(persisterOptions);


            return new SingleTenantInMemoryPersister(new LocalDiskFileManager(diskOptionsAccessor), persisterOptionsAccessor);
        }
    }
}