namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class MultiTenantOnDiskPersister : IPersister {
    long IPersister.Position => throw new NotImplementedException();
    private readonly MultiTenantOnDiskPersisterOptions _options;

    public MultiTenantOnDiskPersister(IOptions<MultiTenantOnDiskPersisterOptions> options) {
        _options = options.Value ?? new MultiTenantOnDiskPersisterOptions();
    }

    public ChannelReader<StreamItem> AllStream => throw new NotImplementedException();

    public ValueTask<WriteResult> AppendToStreamAsync(StreamId id, ExpectedVersion version, EventData[] events) {
        throw new NotImplementedException();
    }

    public void Dispose() {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamId id) {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamKey key) {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamId id, long startingRevision) {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamKey key, long startingRevision) {
        throw new NotImplementedException();
    }

    public ValueTask Truncate() {
        throw new NotImplementedException();
    }
}