namespace LvStreamStore;
public class AdHocHandler<T> : IHandleAsync<T> where T : StreamEvent {
    private readonly Func<T, Task> _handle;

    public AdHocHandler(Func<T, Task> handle) {
        _handle = handle;
    }

    public Task HandleAsync(T msg) => _handle.Invoke(msg);
}