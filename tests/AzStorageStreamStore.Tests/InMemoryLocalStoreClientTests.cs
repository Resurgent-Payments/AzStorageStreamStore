namespace AzStorageStreamStore.Tests;
public class InMemoryLocalStoreClientTests : ClientTestBase<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister => new SingleTenantInMemoryPersister();
}
