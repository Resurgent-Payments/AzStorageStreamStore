namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

public interface IPersister : IDisposable {
    ChannelReader<RecordedEvent> AllStream { get; }

    IAsyncEnumerable<RecordedEvent> ReadAllAsync();
    IAsyncEnumerable<RecordedEvent> ReadAllAsync(long fromPosition);
    IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id);
    IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id, long position);
    IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key);
    IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key, long position);
    ValueTask<WriteResult> WriteAsync(StreamId id, ExpectedVersion version, EventData[] events);
}
