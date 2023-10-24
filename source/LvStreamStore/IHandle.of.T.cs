namespace LvStreamStore;
public interface IHandleAsync<T> where T : Event {
    Task HandleAsync(T msg);
}
