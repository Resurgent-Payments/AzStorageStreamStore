namespace LvStreamStore;
public class AdHocHandler<T> : IHandleAsync<T> where T : StreamEvent {
    private readonly Func<T, ValueTask> _handle;

    public AdHocHandler(Func<T, ValueTask> handle) {
        _handle = handle;
    }

    public ValueTask HandleAsync(T msg) => _handle.Invoke(msg);
}