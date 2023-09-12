namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

public interface IPersister : IDisposable {
    ChannelReader<RecordedEvent> AllStream { get; }

    IAsyncEnumerable<RecordedEvent> ReadAllAsync();
    IAsyncEnumerable<RecordedEvent> ReadAllAsync(long position);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id, long revision);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key, long revision);
    ValueTask<WriteResult> AppendToStreamAsync(StreamId id, ExpectedVersion version, EventData[] events);
}
