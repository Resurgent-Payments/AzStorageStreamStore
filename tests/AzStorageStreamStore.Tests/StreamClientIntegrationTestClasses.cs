namespace AzStorageStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Options;

using Xunit;

[Trait("Type", "Integration")]
public class InMemoryStreamClientIntegrationTests : StreamClientIntegrationTestBase<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister => new SingleTenantInMemoryPersister(new MemoryDataFileManager());
}

[Trait("Type", "Integration")]
public class LocalDiskPersisterIntegrationTests : StreamClientIntegrationTestBase<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister {
        get {
            var options = new LocalDiskFileManagerOptions {
                BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                FileReadBlockSize = 4096 // 4k block size.
            };
            var fake = A.Fake<IOptions<LocalDiskFileManagerOptions>>();
            A.CallTo(() => fake.Value)
                .Returns(options);

            return new SingleTenantInMemoryPersister(new LocalDiskFileManager(fake));
        }
    }
}