namespace LvStreamStore {
    using System;
    using System.Threading.Channels;

    public class InMemoryBus : IDisposable {
        private readonly Channel<BusMessage> _internalBus;
        private readonly List<IHandle<IMessage>> _handlers = new();
        private readonly CancellationTokenSource _cts = new();
        bool _disposed = false;

        public InMemoryBus() {
            _internalBus = Channel.CreateUnbounded<BusMessage>(new UnboundedChannelOptions {
                AllowSynchronousContinuations = false,
                SingleReader = true,
                SingleWriter = false
            });
            _cts.Token.Register(() => _internalBus.Writer.Complete());
            Task.Run(MessagePump, _cts.Token);
        }

        public async Task PublishAsync<T>(T message) where T : IMessage {
            var msg = new BusMessage(new(), message);
            _internalBus.Writer.TryWrite(msg);
            await msg.Tcs.Task;
        }

        public IDisposable SubscribeToAll(IHandle<IMessage> handler) {
            _handlers.Add(handler);
            return new Disposer(() => {
                _handlers.Remove(handler);
                return Unit.Default;
            });
        }

        public IDisposable Subscribe<T>(IHandle<T> handler) where T : IMessage {
            var ahHandler = new AdHocHandler<IMessage>((msg) => handler.Handle((T)msg));
            _handlers.Add(ahHandler);
            return new Disposer(() => {
                _handlers.Remove(ahHandler);
                return Unit.Default;
            });
        }

        public void Dispose() {
            if (_disposed) return;
            _cts.Cancel();
            _disposed = true;
        }

        private async Task MessagePump() {
            await foreach (var msg in _internalBus.Reader.ReadAllAsync()) {
                foreach (var handler in _handlers) {
                    var typeOfHandler = handler.GetType();
                    var genericType = typeOfHandler.GetGenericArguments()[0];

                    if (genericType.IsAssignableFrom(msg.Message.GetType())) {
                        handler.Handle(msg.Message);
                    }
                }

                msg.Tcs.SetResult();
            }
        }

        record BusMessage(TaskCompletionSource Tcs, IMessage Message);
    }
}
