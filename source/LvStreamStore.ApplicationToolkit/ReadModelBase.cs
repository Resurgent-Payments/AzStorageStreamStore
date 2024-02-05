namespace LvStreamStore.ApplicationToolkit;

using System.Collections.ObjectModel;

using LvStreamStore.Messaging;

public class ReadModelBase : IDisposable {
    private readonly Collection<IDisposable> _subscriptions = new();
    protected IStreamStoreRepository Repository { get; init; }
    private readonly AsyncDispatcher _dispatcher;

    public ReadModelBase(AsyncDispatcher dispatcher, IStreamStoreRepository repository) {
        _dispatcher = dispatcher;
        Repository = repository!;
    }

    protected void Subscribe<TCommand>(IHandleAsync<TCommand> handle) where TCommand : Message {
        AsyncHelper.RunSync(() => _dispatcher.HandleAsync(AsyncDispatcher.Register(handle)));
    }

    protected void Subscribe<TEvent>(IReceiver<TEvent> receiver) where TEvent : Event {
        var msg = AsyncDispatcher.Register(receiver);
        AsyncHelper.RunSync(() => _dispatcher.HandleAsync(msg));
        if (msg is IDisposable d) {
            _subscriptions.Add(d);
        }
    }

    protected void SubscribeToStream<TEvent>(IReceiver<TEvent> receiver) where TEvent : Event {
        _subscriptions.Add(Repository.Subscribe(receiver));
    }

    public void Dispose() {
        foreach (var sub in _subscriptions ?? Enumerable.Empty<IDisposable>()) {
            sub?.Dispose();
        }
        _subscriptions?.Clear();
    }
}
