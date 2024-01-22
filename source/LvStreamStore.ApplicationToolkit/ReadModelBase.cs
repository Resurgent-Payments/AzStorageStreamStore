namespace LvStreamStore.ApplicationToolkit;

using System.Collections.ObjectModel;

public class ReadModelBase : IAutoStartService, IDisposable {
    private readonly Collection<IDisposable> _subscriptions = new();
    protected IStreamStoreRepository Repository { get; init; }
    private readonly ISubscriber _inBus;

    public ReadModelBase(ISubscriber inBus, IStreamStoreRepository repository) {
        _inBus = inBus!;
        Repository = repository!;
    }

    protected void Subscribe<TCommand>(IAsyncCommandHandler<TCommand> handle) where TCommand : Command {
        _subscriptions.Add(_inBus.Subscribe(handle));
    }

    protected void Subscribe<TEvent>(IAsyncHandler<TEvent> handle) where TEvent : Event {
        _subscriptions.Add(_inBus.Subscribe(handle));
    }

    protected void SubscribeToStream<TAggregate, TEvent>(IAsyncHandler<TEvent> handler) where TAggregate : AggregateRoot, new() where TEvent : Event {
        _subscriptions.Add(Repository.Subscribe<TAggregate, TEvent>(handler));
    }

    public void Dispose() {
        foreach (var sub in _subscriptions ?? Enumerable.Empty<IDisposable>()) {
            sub?.Dispose();
        }
        _subscriptions?.Clear();
    }
}
