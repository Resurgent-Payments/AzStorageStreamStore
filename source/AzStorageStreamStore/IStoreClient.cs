namespace AzStorageStreamStore;

/// <summary>
///  interface to access the underlying event files.
/// </summary>
/// <remarks>I am not yet sold on this name, and will have to determine if it makes sense or not.</remarks>
public interface IStoreClient : IDisposable {
    Task InitializeAsync();
    ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key);
    IDisposable SubscribeToAll(Action<RecordedEvent> eventHandler);
    IDisposable SubscribeToAllFrom(long position, Action<RecordedEvent> eventHandler);
    IDisposable SubscribeToStream(StreamKey key, Action<RecordedEvent> eventHandler);
    Task<IDisposable> SubscribeToStreamFromAsync(long position, StreamKey key, Action<RecordedEvent> eventHandler);

}