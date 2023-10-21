namespace LvStreamStore;
public interface IHandle<T> where T : Event {
    void Handle(T msg);
}
