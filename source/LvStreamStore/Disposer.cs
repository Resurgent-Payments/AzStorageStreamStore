namespace LvStreamStore;
using System;

public class Disposer : IDisposable {
    Action _onDisposal;

    public Disposer(Action onDisposal) {
        _onDisposal = onDisposal;
    }

    public void Dispose() {
        _onDisposal?.Invoke();
    }
}
