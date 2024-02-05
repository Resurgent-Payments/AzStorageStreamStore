namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class AggregateRoot {
        public Guid Id { get; protected set; }
        internal int Version { get; private set; } = -1;
        private Router Router { get; } = new();
        private Recorder Recorder { get; } = new();

        protected void Register<TMessage>(Action<TMessage> handler) where TMessage : Messaging.Message {
            Router.RegisterRoute(handler);
        }
        protected void Raise(object @object) {
            Recorder.Record(@object);
            Router.Route(@object);
        }

        public void RestoreFromMessages(IEnumerable<Messaging.Message> messages) {
            foreach (var msg in messages) {
                Router.Route(msg);
                Version += 1;
            }
        }

        public object[] TakeMessages() {
            if (Id == Guid.Empty) { throw new InvalidOperationException("Aggregate has not been initialized."); }
            return Recorder.TakeMessages();
        }
    }

    class Router {
        private Dictionary<Type, Action<object>> _routes = new();

        public void RegisterRoute<TMessage>(Action<TMessage> handler) {
            if (_routes.ContainsKey(typeof(TMessage))) throw new RouteRegisteredException();
            _routes.Add(typeof(TMessage), (o) => handler.Invoke((TMessage)o));
        }

        public void Route(object @object) {
            if (!_routes.TryGetValue(@object.GetType(), out var handler)) throw new RouteNotRegisteredException();
            handler.Invoke(@object);
        }
    }

    class Recorder {
        private IList<object> _messages = new List<object>();
        public void Record(object @object) => _messages.Add(@object);

        public object[] TakeMessages() {
            var all = _messages.ToArray();
            _messages.Clear();
            return all;
        }
    }
}
