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
    Task<IDisposable> SubscribeToAllAsync(Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToAllFromAsync(long revision, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamAsync(StreamKey key, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamFromAsync(StreamKey key, long revision, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamFromAsync(StreamId streamId, long revision, Action<RecordedEvent> handler);
}