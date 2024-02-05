namespace LvStreamStore;

using System.Threading;

public abstract class EventStreamReader : IAsyncEnumerable<StreamMessage> {
    public abstract IAsyncEnumerator<StreamMessage> GetAsyncEnumerator(CancellationToken cancellationToken = default);

    public interface IStreamEnumerator : IAsyncEnumerator<StreamMessage> { }
}
