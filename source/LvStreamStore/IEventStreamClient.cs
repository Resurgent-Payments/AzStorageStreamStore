namespace LvStreamStore;
/// <summary>
///  interface to access the underlying event files.
/// </summary>
/// <remarks>I am not yet sold on this name, and will have to determine if it makes sense or not.</remarks>
public interface IEventStreamClient : IDisposable {
    Task InitializeAsync();
    ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events);

    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id);
    IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key);

    IDisposable SubscribeToAll(Action<RecordedEvent> handler);

    IDisposable SubscribeToStream(StreamId streamId, Action<RecordedEvent> handler);
    IDisposable SubscribeToStream(StreamKey key, Action<RecordedEvent> handler);

    Task<IDisposable> SubscribeToStreamFromAsync(StreamId streamId, int revision, Action<RecordedEvent> handler);
    Task<IDisposable> SubscribeToStreamFromAsync(StreamKey key, int revision, Action<RecordedEvent> handler);
}