namespace LvStreamStore {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    internal class Bus : IDisposable {
        private readonly Channel<Message> _messageChannel = Channel.CreateUnbounded<Message>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false
        });
        private readonly Dictionary<Guid, TaskCompletionSource<CommandResult>> _pending = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly List<Action<object>> _eventSubscriptions = new();
        private readonly Dictionary<Type, Func<object, Task<CommandResult>>> _commandSubscriptions = new();
        long _numberOfEventsPublished = 0;
        long _averageExecutionTimeTicks = 0;


        public Bus() {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
            Task.Run(async () => { while (!_cts.IsCancellationRequested) { await timer.WaitForNextTickAsync(); PublishTelemetry(); } }, _cts.Token);
            Task.Run(Pump, _cts.Token);
        }

        public async Task PublishAsync<T>(T @event) where T : Event {
            await _messageChannel.Writer.WriteAsync(@event);
        }

        public Task<CommandResult> RunAsync<T>(T command) where T : Command => RunAsync(command, TimeSpan.FromSeconds(30));

        public async Task<CommandResult> RunAsync<T>(T command, TimeSpan timesOutAfter) where T : Command {
            var tcs = new TaskCompletionSource<CommandResult>();
            _pending.Add(command.MsgId!.Value, tcs);
            await _messageChannel.Writer.WriteAsync(command);
            return await tcs.Task.WithTimeout(timesOutAfter);
        }

        public IDisposable Subscribe<T>(IHandle<T> handler) where T : Event {
            var action = new Action<object>((o) => handler.Handle((T)o));
            _eventSubscriptions.Add(action);
            return new Disposer(() => _eventSubscriptions.Remove(action));
        }

        public IDisposable Subscribe<T>(IHandleCommand<T> handler) where T : Command {
            var cmdType = typeof(T);
            var func = new Func<object, Task<CommandResult>>((o) => handler.HandleAsync((T)o));
            _commandSubscriptions.Add(cmdType, func);
            return new Disposer(() => _commandSubscriptions.Remove(cmdType));
        }

        private async void Pump() {
            Debug.WriteLine("Pump online.");
            while (!_cts.IsCancellationRequested) {
                Debug.WriteLine("Waiting for message...");
                await _messageChannel.Reader.WaitToReadAsync();

                _numberOfEventsPublished++;
                var startTime = DateTime.UnixEpoch.Ticks;

                var msg = await _messageChannel.Reader.ReadAsync();
                Debug.WriteLine("Received message.");

                switch (msg) {
                    case Event @event:
                        Debug.WriteLine("Received an event.");
                        foreach (var evtHandler in _eventSubscriptions) {
                            try {
                                Debug.WriteLine("Handling event.");
                                evtHandler.Invoke(@event);
                            }
                            catch (Exception exc) {
                                Debug.WriteLine($"{exc.GetType().Name}: {exc.Message}");
                            }
                        }
                        Debug.WriteLine("Event handled.");
                        break;
                    case Command command:
                        Debug.WriteLine("Received Command");
                        if (!_commandSubscriptions.TryGetValue(command.GetType(), out var cmdHandler)) { throw new NotSupportedException(); }
                        if (!_pending.TryGetValue(command.MsgId!.Value, out var tcs)) { throw new NotSupportedException(); }

                        try {
                            var result = await cmdHandler.Invoke(command);
                            tcs.SetResult(result);
                            Debug.WriteLine("Command handled");
                        }
                        catch (Exception exc) {
                            tcs.SetResult(command.Fail(exc));
                        }
                        finally {
                            _pending.Remove(command.MsgId!.Value);
                            if (!tcs.Task.IsCompleted) {
                                tcs.SetCanceled();
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

        private async void PublishTelemetry() {
            // question: is there a better way to ensure this works as you'd expect to avoid not losing the telemetry info?
            var telemetry = new BusTelemetry(_numberOfEventsPublished, _averageExecutionTimeTicks);
            _numberOfEventsPublished = 0;
            _averageExecutionTimeTicks = 0;
            await _messageChannel.Writer.WriteAsync(telemetry);
        }

        public void Dispose() {
            _cts?.Dispose();
        }
    }
}
