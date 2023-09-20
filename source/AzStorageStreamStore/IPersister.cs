namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;

public interface IPersister : IDisposable {
    internal long Position { get; }
    ChannelReader<StreamItem> AllStream { get; }

    IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamId id);
    IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamId id, long startingRevision);

    /// <summary>
    /// Reads a stream based on it's "Key"
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>This creates a stream similar to EventStore DB's $ce- stream type.  If you want to read all events (akin to reading $all in EventStore DB), you'll pass in <see cref="StreamKey.All"/></para>
    /// <para>Note: For any multi-tenant scenarios, the value in Categories[0] will be used, in all cases, to differentiate between tenants.  Any attempt to read using <see cref="StreamKey.All"/> will throw an <see cref="InvalidKeyException"/></para>
    /// </remarks>
    IAsyncEnumerable<StreamItem> ReadStreamAsync(StreamKey key);

    /// <summary>
    /// Reads a stream based on it's "Key", and from its revision within the "key" stream.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="revision"></param>
    /// <returns></returns>
    /// <remarks>
    /// <para>This creates a stream similar to EventStore DB's $ce- stream type.  If you want to read all events (akin to reading $all in EventStore DB), you'll pass in <see cref="StreamKey.All"/></para>
    /// <para>Note: For any multi-tenant scenarios, the value in Categories[0] will be used, in all cases, to differentiate between tenants.  Any attempt to read using <see cref="StreamKey.All"/> will throw an <see cref="InvalidKeyException"/></para>
    /// </remarks>
    IAsyncEnumerable<StreamItem> ReadStreamFromAsync(StreamKey key, long startingRevision);

    /// <summary>
    /// Writes a series of events to the main timeline stream (EventStore DB calls this the $all stream)
    /// </summary>
    /// <param name="id"></param>
    /// <param name="version"></param>
    /// <param name="events"></param>
    /// <returns></returns>
    ValueTask<WriteResult> AppendToStreamAsync(StreamId id, ExpectedVersion version, EventData[] events);

    ValueTask Truncate();
}
