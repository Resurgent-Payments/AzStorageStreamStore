namespace LvStreamStore.Messaging;

using System;
using System.Threading.Tasks;

public class AdHocReceiver<T> : IReceiver<T> where T : Message {
    Func<T, Task> _inner;

    public AdHocReceiver(Func<T, Task> inner) {
        _inner = inner;
    }

    public Task Receive(T msg)
        => _inner(msg);
}
