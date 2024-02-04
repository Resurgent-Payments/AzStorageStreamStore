namespace LvStreamStore.Messaging;
public interface IHandleAsync<T> where T : Message {
    Task HandleAsync(T message);
}
