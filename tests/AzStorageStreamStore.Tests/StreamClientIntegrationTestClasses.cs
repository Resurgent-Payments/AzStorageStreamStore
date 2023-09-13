namespace AzStorageStreamStore.Tests;

using FakeItEasy;

using Microsoft.Extensions.Options;

using Xunit;

[Trait("Type", "Integration")]
public class InMemoryStreamClientIntegrationTests : StreamClientIntegrationTestBase<InMemoryPersister> {
    protected override InMemoryPersister Persister => new InMemoryPersister();
}

[Trait("Type", "Integration")]
public class LocalDiskPersisterIntegrationTests : StreamClientIntegrationTestBase<SingleTenantOnDiskPersister> {
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