namespace AzStorageStreamStore.Tests;

public class LocalClientWithMultiTenantInMemoryPersisterTests : ClientTestBase<MultiTenantInMemoryPersister> {
    protected override MultiTenantInMemoryPersister Persister => throw new NotImplementedException();
}