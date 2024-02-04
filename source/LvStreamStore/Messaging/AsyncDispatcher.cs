namespace LvStreamStore.Messaging {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class AsyncDispatcher : IHandleAsync<Message>, IHandleAsync<AsyncDispatcher.RegisterMessage>, IAsyncPublisher, IAsyncSubscriber<Message> {
        private readonly Dictionary<Type, AsyncHandler> _handlers = new();

        public AsyncDispatcher() {
            _handlers[typeof(RegisterMessage)] = new AsyncHandler<RegisterMessage>(this);
        }

        public async Task HandleAsync(Message message) {
            if (!_handlers.TryGetValue(message.GetType(), out var handler)) { throw new MissingHandlerException(); }

            await handler.HandleAsync(message);
        }

        public static Message Register<T>(IHandleAsync<T> handler) where T : Message
            => new RegisterMessage(typeof(T), new AsyncHandler<T>(handler));

        Task IHandleAsync<RegisterMessage>.HandleAsync(RegisterMessage message) {
            _handlers[message.Type] = message.Wrapper;
            return Task.CompletedTask;
        }

        public Task PublishAsync<TMessage>(TMessage msg) where TMessage : Message {
            throw new NotImplementedException();
        }

        public IDisposable Subscribe(IHandleAsync<Message> handler) {
            throw new NotImplementedException();
        }

        record RegisterMessage(Type Type, AsyncHandler Wrapper) : Message;
    }
}
