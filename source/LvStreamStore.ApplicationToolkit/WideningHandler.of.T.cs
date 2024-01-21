namespace LvStreamStore.ApplicationToolkit {
    using System.Threading.Tasks;

    public class WideningHandler<TEvent> : IAsyncHandler<Event> where TEvent : Event {
        IAsyncHandler<TEvent> _handler;
        public WideningHandler(IAsyncHandler<TEvent> handler) {
            _handler = handler;
        }

        public ValueTask HandleAsync(Event @event) {
            return @event.GetType() == typeof(TEvent)
                ? _handler.HandleAsync((TEvent)@event)
                : ValueTask.CompletedTask;
        }
    }
}
