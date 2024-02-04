namespace LvStreamStore.Messaging;

public interface IAsyncSubscriber<TMessage> where TMessage : Message {
    IDisposable Subscribe(IHandleAsync<TMessage> handler);
}

public interface IAsyncPublisher {
    Task PublishAsync<TMessage>(TMessage msg) where TMessage : Message;
}