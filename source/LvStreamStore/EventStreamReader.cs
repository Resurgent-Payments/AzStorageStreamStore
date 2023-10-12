namespace LvStreamStore;

using System.Threading;

public abstract class EventStreamReader : IAsyncEnumerable<StreamItem> {
    public abstract IAsyncEnumerator<StreamItem> GetAsyncEnumerator(CancellationToken cancellationToken = default);

    public interface IEnumerator : IAsyncEnumerator<StreamItem> {
        int Position { get; }
        int Offset { get; }
    }
}
