namespace LvStreamStore.Messaging;

using System.Threading.Tasks;

internal record AsyncHandler<T> : AsyncHandler where T : Message {
    IHandleAsync<T> _inner;

    public AsyncHandler(IHandleAsync<T> inner) {
        _inner = inner;
    }

    public override async ValueTask HandleAsync(Message msg) {
        if (!(msg is T @event)) { return; }
        await _inner.HandleAsync(@event);
    }
}
