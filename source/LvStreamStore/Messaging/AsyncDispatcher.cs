namespace LvStreamStore.Messaging {
    using System;
    using System.Collections.Generic;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public class AsyncDispatcher : IHandleAsync<Message>,
        IHandleAsync<AsyncDispatcher.RegisterMessage>,
        IHandleAsync<AsyncDispatcher.RegisterReceiver>,
        IBroadcastAsync<Message>,
        ISendAsync<Message> {
        private readonly ILogger _log;
        private readonly Dictionary<Type, AsyncHandler> _handlers = new();
        private readonly List<Receiver> _receivers = new();
        private readonly Channel<BroadcastArgs> _enqueueForBroadcast;

        public AsyncDispatcher(ILoggerFactory? loggerFactory = null) {
            loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _log = loggerFactory.CreateLogger<AsyncDispatcher>();
            _handlers[typeof(RegisterMessage)] = new AsyncHandler<RegisterMessage>(this);
            _handlers[typeof(RegisterReceiver)] = new AsyncHandler<RegisterReceiver>(this);
            _enqueueForBroadcast = Channel.CreateUnbounded<BroadcastArgs>(new UnboundedChannelOptions {
                SingleReader = true,
                SingleWriter = true
            });
            BroadcastMessages();
        }

        public async Task HandleAsync(Message message) {
            if (!_handlers.TryGetValue(message.GetType(), out var handler)) { throw new MissingHandlerException(); }

            await handler.HandleAsync(message);
        }

        public static Message Register<T>(IHandleAsync<T> handler) where T : Message
            => new RegisterMessage(typeof(T), new AsyncHandler<T>(handler));
        public static Message Register<T>(IReceiver<T> receiver) where T : Message
            => new RegisterReceiver(typeof(T), new Receiver<T>(receiver));

        Task IHandleAsync<RegisterMessage>.HandleAsync(RegisterMessage message) {
            _handlers[message.Type] = message.Wrapper;
            return Task.CompletedTask;
        }
        Task IHandleAsync<RegisterReceiver>.HandleAsync(RegisterReceiver receiver) {
            receiver.OnDisposal = () => _receivers.Remove(receiver.Wrapper);
            _receivers.Add(receiver.Wrapper);
            return Task.CompletedTask;
        }

        public Task SendAsync(Message msg)
            => HandleAsync(msg);

        public Task BroadcastAsync(Message message) {
            var tcs = new TaskCompletionSource();
            if (!_enqueueForBroadcast.Writer.TryWrite(new BroadcastArgs(message, tcs))) { throw new InvalidOperationException(); }
            return tcs.Task;
        }

        private record RegisterMessage(Type Type, AsyncHandler Wrapper) : Message;
        private record RegisterReceiver(Type Type, Receiver Wrapper) : Message, IDisposable {
            public Action? OnDisposal { get; set; } = null;
            public void Dispose() => OnDisposal?.Invoke();
        }

        private record BroadcastArgs(Message Message, TaskCompletionSource CompletionSource);

        private async void BroadcastMessages() {
            await Task.Yield();
            while (await _enqueueForBroadcast.Reader.WaitToReadAsync()) {
                var args = await _enqueueForBroadcast.Reader.ReadAsync();
                for (var idx = 0; idx < _receivers.Count; idx++) {
                    try {
                        _receivers[idx].Receive(args.Message);
                    }
                    catch (Exception exc) {
                        _log.LogWarning(exc, "Broadcast receiver encountered an error.");
                    }
                }
                args.CompletionSource.SetResult();
            }
        }
    }
}
