namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading.Tasks;

    public class AsyncAdHocHandler<TEvent> : IAsyncHandler<TEvent> where TEvent : Event {
        private readonly Func<TEvent, ValueTask> _handles;

        public AsyncAdHocHandler(Func<TEvent, ValueTask> handles) {
            _handles = handles;
        }

        public async ValueTask HandleAsync(TEvent @event) {
            await _handles(@event);
        }
    }
}
