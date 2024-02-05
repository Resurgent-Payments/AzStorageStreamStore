namespace LvStreamStore.Messaging {
    using System;
    using System.Threading.Tasks;

    internal class AdHocHandler<T> : IHandleAsync<T> where T : Message {
        Func<T, Task> _inner;

        public AdHocHandler(Func<T, Task> inner) {
            _inner = inner;
        }

        public Task HandleAsync(T message)
            => _inner(message);
    }
}
