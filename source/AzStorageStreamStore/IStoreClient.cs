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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    /// <remarks>Do not use this witin your system if you think you want to support tenant-per-data style programming.</remarks>
    Task<IDisposable> SubscribeToAllAsync(Action<RecordedEvent> handler);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler"></param>
    /// <returns></returns>
    /// <remarks>Do not use this witin your system if you think you want to support tenant-per-data style programming.</remarks>
    Task<IDisposable> SubscribeToAllFromAsync(int posititon, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamAsync(StreamKey key, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamFromAsync(StreamKey key, int revision, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamFromAsync(StreamId streamId, int revision, Action<RecordedEvent> handler);
}