namespace LvStreamStore.Messaging {
    using System;
    using System.Threading.Tasks;

    internal class FilteredReceiver<T> : IReceiver<T> where T : Message {
        IReceiver<T> _inner;
        Func<T, bool> _filter;

        public FilteredReceiver(IReceiver<T> inner, Func<T, bool> filter) {
            _inner = inner;
            _filter = filter;
        }

        public async Task Receive(T msg) {
            if (_filter(msg)) {
                await _inner.Receive(msg);
            }
        }
    }
}
