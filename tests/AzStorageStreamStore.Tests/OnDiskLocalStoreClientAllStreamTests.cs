namespace AzStorageStreamStore.Tests;

using AzStorageStreamStore;

using FakeItEasy;

using Microsoft.Extensions.Options;

public class OnDiskLocalStoreClientAllStreamTests : LocalStoreClientAllStreamTests<SingleTenantInMemoryPersister> {
    protected override SingleTenantInMemoryPersister Persister {
        get {
            var diskOptions = new LocalDiskFileManagerOptions {
                BaseDataPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                FileReadBlockSize = 4096 // 4k block size.
            };
            var diskOptionsAccessor = A.Fake<IOptions<LocalDiskFileManagerOptions>>();
            A.CallTo(() => diskOptionsAccessor.Value)
                .Returns(diskOptions);

            var persisterOptions = new SingleTenantInMemoryPersisterOptions();
            var persisterOptionsAccessor = A.Fake<IOptions<SingleTenantInMemoryPersisterOptions>>();
            A.CallTo(() => persisterOptionsAccessor.Value)
                .Returns(persisterOptions);


            return new SingleTenantInMemoryPersister(new LocalDiskFileManager(diskOptionsAccessor), persisterOptionsAccessor);
        }
    }
}