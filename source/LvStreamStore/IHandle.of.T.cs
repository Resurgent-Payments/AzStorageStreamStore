namespace LvStreamStore;
public interface IHandleAsync<T> where T : StreamEvent {
    Task HandleAsync(T msg);
}
