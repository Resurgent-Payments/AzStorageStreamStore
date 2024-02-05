namespace LvStreamStore.ApplicationToolkit.WebHooks;

using LvStreamStore.Messaging;

//todo: need to make this internal and use it as part of a fluent configuration.
public class SubscriptionService : TransientSubscriber,
    IHandleAsync<SubscriptionMsgs.Subscribe>,
    IHandleAsync<SubscriptionMsgs.Enable>,
    IHandleAsync<SubscriptionMsgs.Disable>,
    IHandleAsync<SubscriptionMsgs.Remove> {
    IStreamStoreRepository _repository;

    public SubscriptionService(AsyncDispatcher dispatcher, IStreamStoreRepository repository) : base(dispatcher) {
        _repository = repository;
        Subscribe<SubscriptionMsgs.Subscribe>(this);
        Subscribe<SubscriptionMsgs.Enable>(this);
        Subscribe<SubscriptionMsgs.Disable>(this);
        Subscribe<SubscriptionMsgs.Remove>(this);
    }

    public async Task HandleAsync(SubscriptionMsgs.Subscribe command) {
        await _repository.Save(new Subscription(command.SubscriptionId, command.WebHookId, command.Description, command.PostUrl));
    }

    public async Task HandleAsync(SubscriptionMsgs.Enable command) {
        var subscription = await _repository.TryGetById<Subscription>(command.SubscriptionId);
        subscription.Enable();
    }

    public async Task HandleAsync(SubscriptionMsgs.Disable command) {
        var subscription = await _repository.TryGetById<Subscription>(command.SubscriptionId);
        subscription.Disable();

        await _repository.Save(subscription);
    }

    public async Task HandleAsync(SubscriptionMsgs.Remove command) {
        var subscription = await _repository.TryGetById<Subscription>(command.SubscriptionId);
        subscription.Remove();

        await _repository.Save(subscription);
    }
}