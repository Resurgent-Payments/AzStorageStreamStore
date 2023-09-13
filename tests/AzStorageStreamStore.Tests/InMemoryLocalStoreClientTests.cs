namespace AzStorageStreamStore.Tests;
public class InMemoryLocalStoreClientTests : LocalStoreClientTestBase<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister => new SingleTenantInMemoryPersister();
}
