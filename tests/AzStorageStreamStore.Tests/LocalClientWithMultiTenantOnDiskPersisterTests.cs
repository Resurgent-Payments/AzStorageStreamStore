namespace AzStorageStreamStore.Tests;

public class LocalClientWithMultiTenantOnDiskPersisterTests : ClientTestBase<MultiTenantPersister> {
    protected override MultiTenantPersister Persister => throw new NotImplementedException();
}