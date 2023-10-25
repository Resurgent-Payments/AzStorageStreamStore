namespace LvStreamStore.ApplicationToolkit {
    using System;

    public class Disposer : IDisposable {
        private readonly Action _onDisposal;
        private bool _disposed = false;

        public Disposer(Action onDisposal) {
            _onDisposal = onDisposal;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposing || _disposed) return;

            _onDisposal.Invoke();

            _disposed = true;
        }
    }
}
