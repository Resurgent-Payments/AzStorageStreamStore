namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

public class MultiTenantInMemoryPersister : IPersister {
    long IPersister.Position => throw new NotImplementedException();

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

    public IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamId id, int start) {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamKey key, int start) {
        throw new NotImplementedException();
    }

    public ValueTask Truncate() {
        throw new NotImplementedException();
    }
}