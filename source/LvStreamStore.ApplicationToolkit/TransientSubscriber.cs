namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.Extensions.Logging;

    public abstract class TransientSubscriber : IDisposable {
        private readonly Collection<IDisposable> _subscriptions = new();
        private readonly IDispatcher _dispatcher;
        private readonly ILogger _log;

        public TransientSubscriber(IDispatcher dispatcher, ILoggerFactory factory) {
            _dispatcher = dispatcher;
            _log = factory.CreateLogger(GetType());
        }

        protected void Subscribe<TCommand>(IAsyncCommandHandler<TCommand> handler) where TCommand : Command {
            _subscriptions.Add(_dispatcher.Subscribe(handler));
        }

        //note: we may need to provide another subscribe here to respond to committed events from the underlying stream.
        protected void Subscribe<TEvent>(IAsyncHandler<TEvent> handler) where TEvent : Event {
            _subscriptions.Add(_dispatcher.Subscribe(handler));
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
