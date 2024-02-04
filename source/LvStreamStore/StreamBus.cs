namespace LvStreamStore;

using System.Threading.Tasks;

class AsyncStreamBus : IHandleAsync<StreamEvent>, IHandleAsync<AsyncStreamBus.RegisterStreamEventHandler> {
    private readonly IDictionary<Type, Envelope> _handlers = new Dictionary<Type, Envelope>();
    public AsyncStreamBus() {
        _handlers[typeof(RegisterStreamEventHandler)] = new Envelope<RegisterStreamEventHandler>(this);
    }

    public async ValueTask HandleAsync(StreamEvent msg) {
        if (_handlers.TryGetValue(msg.GetType(), out var handler)) {
            await handler.SendAsync(msg);
        }
    }

    public static StreamEvent Register<T>(IHandleAsync<T> handler) where T : StreamEvent
        => new RegisterStreamEventHandler(typeof(T), new Envelope<T>(handler));

    ValueTask IHandleAsync<RegisterStreamEventHandler>.HandleAsync(RegisterStreamEventHandler msg) {
        _handlers.Add(msg.type, msg.Wrapper);
        return ValueTask.CompletedTask;
    }

    record RegisterStreamEventHandler(Type type, Envelope Wrapper) : StreamEvent;

    abstract record Envelope : StreamEvent {
        public abstract ValueTask SendAsync(StreamEvent msg);
    }

    record Envelope<T> : Envelope where T : StreamEvent {
        private readonly IHandleAsync<T> _handler;

        public Envelope(IHandleAsync<T> handler) {
            _handler = handler;
        }

        public override async ValueTask SendAsync(StreamEvent @event) {
            if (!(@event is T e)) { return; }
            await _handler.HandleAsync(e);
        }
    }
}