namespace LvStreamStore.Messaging;

public interface IReceiver<T> where T : Message {
    void Receive(T msg);
}

internal abstract record Receiver {
    public abstract void Receive(Message msg);
}

internal record Receiver<T> : Receiver where T : Message {
    private readonly IReceiver<T> _inner;

    public Receiver(IReceiver<T> inner) {
        _inner = inner;
    }

    public override void Receive(Message msg) {
        if (msg is not T @event) { return; }
        _inner.Receive(@event);
    }
}


