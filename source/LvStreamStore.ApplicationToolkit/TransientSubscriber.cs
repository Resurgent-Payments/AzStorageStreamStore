namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Collections.ObjectModel;

    using LvStreamStore.Messaging;

    public abstract class TransientSubscriber : IDisposable {
        private readonly Collection<IDisposable> _subscriptions = new();
        private readonly AsyncDispatcher _dispatcher;

        public TransientSubscriber(AsyncDispatcher dispatcher) {
            _dispatcher = dispatcher;
        }

        protected void Subscribe<TCommand>(IHandleAsync<TCommand> handler) where TCommand : Message {
            AsyncHelper.RunSync(() => _dispatcher.HandleAsync(AsyncDispatcher.Register(handler)));
        }

        //note: we may need to provide another subscribe here to respond to committed events from the underlying stream.
        protected void Subscribe<TEvent>(IReceiver<TEvent> receiver) where TEvent : Event {
            var x = AsyncDispatcher.Register(receiver);
            AsyncHelper.RunSync(() => _dispatcher.HandleAsync(x));
            if (x is IDisposable d) {
                _subscriptions.Add(d);
            }
        }

        bool _disposed = false;
        public void Dispose() {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing) {
            if (_disposed || !disposing) { return; }
            foreach (var subscription in _subscriptions) { subscription.Dispose(); }
            _disposed = true;
        }
    }
}
