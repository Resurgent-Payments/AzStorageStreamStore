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

    Task<IDisposable> SubscribeToStreamAsync(Messaging.IReceiver<StreamItem> handler);

    Task<IDisposable> SubscribeToStreamAsync(StreamKey streamKey, Messaging.IReceiver<StreamItem> handler);

    Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, Messaging.IReceiver<StreamItem> handler);
}