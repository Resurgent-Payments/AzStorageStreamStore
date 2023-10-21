namespace LvStreamStore;
public class AdHocHandler<T> : IHandle<T> where T : Event {
    private readonly Action<T> _handle;

    public AdHocHandler(Action<T> handle) {
        _handle = handle;
    }

    public void Handle(T msg) {
        _handle.Invoke(msg);
    }
}