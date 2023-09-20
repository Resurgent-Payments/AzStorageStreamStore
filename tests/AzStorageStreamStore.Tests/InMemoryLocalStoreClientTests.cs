namespace AzStorageStreamStore.Tests;
public class LocalClientWithSingleTenantInMemoryPersisterTests : ClientTestBase<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister => new SingleTenantInMemoryPersister();
}
