namespace AzStorageStreamStore;

/// <summary>
///  interface to access the underlying event files.
/// </summary>
/// <remarks>I am not yet sold on this name, and will have to determine if it makes sense or not.</remarks>
public interface IStoreClient : IDisposable {
    Task InitializeAsync();
    Task<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key);
    void SubscribeToAll(Action<RecordedEvent> eventHandler);
    void SubscribeToAllFrom(long position, Action<RecordedEvent> eventHandler);
    void SubscribeToStream(StreamKey key, Action<RecordedEvent> eventHandler);
    void SubscribeToStreamFrom(long position, StreamKey key, Action<RecordedEvent> eventHandler);

}