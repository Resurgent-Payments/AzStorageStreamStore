namespace AzStorageStreamStore.Tests;
public class InMemoryLocalStoreClientTests : LocalStoreClientTestBase<InMemoryPersister> {
    protected override InMemoryPersister Persister => new InMemoryPersister();
}
