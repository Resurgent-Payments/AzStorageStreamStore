namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    public class Dispatcher : IDispatcher, IDisposable {
        private readonly ILogger _logger;
        private readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false
        });
        private readonly Dictionary<Guid, TaskCompletionSource<CommandResult>> _pending = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Dictionary<Type, HashSet<Func<object, Task>>> _eventSubscriptions = new();
        private readonly Dictionary<Type, Func<Command, Task<CommandResult>>> _commandSubscriptions = new();


        long _numberOfEventsPublished = 0;
        long _averageExecutionTimeTicks = 0;

        public Dispatcher(ILoggerFactory loggerFactory) {
            _logger = loggerFactory.CreateLogger(typeof(Dispatcher));
            Task.Run(Pump);
        }

        /// <inheritdoc/>
        public async ValueTask PublishAsync<TMessage>(TMessage @event) where TMessage : Message => await _messageChannel.Writer.WriteAsync(@event);

        /// <inheritdoc/>
        public IDisposable Subscribe<TEvent>(IAsyncHandler<TEvent> handler) where TEvent : Message {
            if (!_eventSubscriptions.TryGetValue(typeof(TEvent), out var handlers)) {
                handlers = new();
                _eventSubscriptions.Add(typeof(TEvent), handlers);
            }
            Func<object, Task> handlerImpl = async (o) => await handler.HandleAsync((TEvent)o);
            handlers.Add(handlerImpl);
            return new Disposer(() => handlers.Remove(handlerImpl));
        }

        public IDisposable Subscribe<TCommand>(IAsyncCommandHandler<TCommand> handler) where TCommand : Command {
            var commandType = typeof(TCommand);
            if (_commandSubscriptions.ContainsKey(commandType)) throw new CommandHandlerRegistrationExistsException();
            Func<Command, Task<CommandResult>> handlerImpl = async (o) => await handler.HandleAsync((TCommand)o);
            _commandSubscriptions.Add(commandType, handlerImpl);
            return new Disposer(() => _commandSubscriptions.Remove(commandType));
        }

        public ValueTask<CommandResult> SendAsync<TCommand>(TCommand command) where TCommand : Command
            => SendAsync(command, TimeSpan.FromSeconds(30));

        /// <inheritdoc/>
        public async ValueTask<CommandResult> SendAsync<TCommand>(TCommand command, TimeSpan timeout) where TCommand : Command {
            var tcs = new TaskCompletionSource<CommandResult>();
            _pending.Add(command.MsgId!.Value, tcs);
            await _messageChannel.Writer.WriteAsync(command);
            return await tcs.Task.WithTimeout(timeout);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        bool _isDisposed = false;
        protected virtual void Dispose(bool disposing) {
            if (!disposing || _isDisposed) return;

            _messageChannel.Writer.Complete();
            _cts?.Cancel();

            _isDisposed = true;
        }

        private async void Pump() {
            _logger.LogDebug("Pump online.");
            while (!_cts.IsCancellationRequested) {
                _logger.LogDebug("Waiting for message...");
                await _messageChannel.Reader.WaitToReadAsync();

                _numberOfEventsPublished++;
                var startTime = DateTime.UnixEpoch.Ticks;

                Message? msg;
                try {
                    msg = await _messageChannel.Reader.ReadAsync();
                }
                catch (ChannelClosedException closedEx) {
                    _logger.LogDebug(closedEx, "Channel has been closed.  Pump is most likely shutting down.");
                    break;
                }

                _logger.LogDebug("Received message.");

                switch (msg) {
                    case Event @event:
                        if (!_eventSubscriptions.TryGetValue(@event.GetType(), out var handles)) { continue; }
                        _logger.LogDebug("Received an event.");
                        foreach (var evtHandler in handles) {
                            try {
                                _logger.LogDebug("Handling event.");
                                await evtHandler.Invoke(@event);
                            }
                            catch (Exception exc) {
                                _logger.LogDebug($"{exc.GetType().Name}: {exc.Message}");
                            }
                        }
                        _logger.LogDebug("Event handled.");
                        break;
                    case Command command:
                        _logger.LogDebug("Received Command");
                        TaskCompletionSource<CommandResult>? tcs = null;
                        try {
                            if (!_pending.TryGetValue(command.MsgId!.Value, out tcs)) { throw new NotSupportedException(); }
                            if (!_commandSubscriptions.TryGetValue(command.GetType(), out var cmdHandler)) { throw new MissingCommandHandlerException(); }
                            var result = await cmdHandler.Invoke(command);
                            tcs.SetResult(result);
                            _logger.LogDebug("Command handled");
                        }
                        catch (Exception exc) {
                            tcs?.SetException(exc);
                        }
                        finally {
                            _pending.Remove(command.MsgId!.Value);
                            if (!tcs?.Task.IsCompleted ?? false) {
                                tcs?.SetCanceled();
                            }
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }

                var endTime = DateTime.UnixEpoch.Ticks;

                _averageExecutionTimeTicks += (endTime - startTime) / 2;
            }
        }
    }
}
