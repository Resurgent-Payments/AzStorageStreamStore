namespace LvStreamStore;

public abstract class EventStreamObserver : IObservable<StreamItem>, IDisposable {
    private readonly List<IObserver<StreamItem>> _subscribers = new();

    public IDisposable Subscribe(IObserver<StreamItem> observer) {
        _subscribers.Add(observer);
        return new StreamDisposer(() => _subscribers.Remove(observer));
    }

    protected void Publish(StreamItem item) {
        foreach (var subscriber in _subscribers) {
            subscriber.OnNext(item);
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool _disposed = false;
    protected virtual void Dispose(bool disposing) {
        if (_disposed || !disposing) return;

        _subscribers.Clear();

        _disposed = true;
    }
}
