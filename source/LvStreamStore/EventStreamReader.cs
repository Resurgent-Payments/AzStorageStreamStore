namespace LvStreamStore;

public interface EventStreamReader : IAsyncEnumerator<StreamItem> {
    int Position { get; }
    int Offset { get; }
}