namespace LvStreamStore.Messaging;

public interface IReceiver<T> where T : Message {
    Task Receive(T msg);
}

internal abstract record Receiver {
    public abstract Task Receive(Message msg);
}

internal record Receiver<T> : Receiver where T : Message {
    private readonly IReceiver<T> _inner;

    public Receiver(IReceiver<T> inner) {
        _inner = inner;
    }

    public override Task Receive(Message msg) {
        if (msg is T @event) { return _inner.Receive(@event); }

        return Task.CompletedTask;
    }
}


