namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

public class InMemoryLocalStoreClientAllStreamTests : LocalStoreClientAllStreamTests<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister => new SingleTenantInMemoryPersister(new MemoryDataFileManager());
}
