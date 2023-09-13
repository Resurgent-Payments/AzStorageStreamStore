namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

public class InMemoryLocalStoreClientAllStreamTests : LocalStoreClientAllStreamTests<InMemoryPersister> {
    protected override InMemoryPersister Persister => new InMemoryPersister();
}
