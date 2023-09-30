namespace LvStreamStore;

sealed class StreamDisposer : IDisposable {
    private readonly Action _ondisposal;
    private bool _disposed = false;
    public StreamDisposer(Action onDisposal) {
        _ondisposal = onDisposal;
    }
    public void Dispose() {
        if (_disposed) return;

        _ondisposal.Invoke();
        _disposed = true;
    }
}