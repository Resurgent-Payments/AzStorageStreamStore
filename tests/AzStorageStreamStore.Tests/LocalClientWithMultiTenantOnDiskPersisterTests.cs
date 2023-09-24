namespace AzStorageStreamStore.Tests;

public class LocalClientWithMultiTenantOnDiskPersisterTests : ClientTestBase<MultiTenantOnDiskPersister> {
    protected override MultiTenantOnDiskPersister Persister => throw new NotImplementedException();
}