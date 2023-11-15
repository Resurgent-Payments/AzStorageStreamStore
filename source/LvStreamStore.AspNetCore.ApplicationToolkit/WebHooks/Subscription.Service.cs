namespace LvStreamStore.ApplicationToolkit.WebHooks;

//todo: need to make this internal and use it as part of a fluent configuration.
public class SubscriptionService :
    IAutoStartService, // service locator interface.
    IAsyncCommandHandler<SubscriptionMsgs.Subscribe>,
    IAsyncCommandHandler<SubscriptionMsgs.Enable>,
    IAsyncCommandHandler<SubscriptionMsgs.Disable>,
    IAsyncCommandHandler<SubscriptionMsgs.Remove> {
    IStreamStoreRepository _repository;

    public SubscriptionService(IStreamStoreRepository repository, IDispatcher dispatcher) {
        _repository = repository;
        dispatcher.Subscribe<SubscriptionMsgs.Subscribe>(this);
        dispatcher.Subscribe<SubscriptionMsgs.Enable>(this);
        dispatcher.Subscribe<SubscriptionMsgs.Disable>(this);
        dispatcher.Subscribe<SubscriptionMsgs.Remove>(this);
    }

    public async ValueTask<CommandResult> HandleAsync(SubscriptionMsgs.Subscribe command) {
        try {
            return await _repository.Save(new Subscription(command.SubscriptionId, command.WebHookId, command.Description, command.PostUrl))
                ? command.Complete()
                : command.Fail();
        }
        catch (Exception ex) {
            return command.Fail(ex);
        }
    }

    public async ValueTask<CommandResult> HandleAsync(SubscriptionMsgs.Enable command) {
        try {
            var subscription = await _repository.TryGetById<Subscription>(command.SubscriptionId);
            subscription.Enable();

            return await _repository.Save(subscription)
                ? command.Complete()
                : command.Fail();
        }
        catch (Exception exc) {
            return command.Fail(exc);
        }
    }

    public async ValueTask<CommandResult> HandleAsync(SubscriptionMsgs.Disable command) {
        try {
            var subscription = await _repository.TryGetById<Subscription>(command.SubscriptionId);
            subscription.Disable();

            return await _repository.Save(subscription)
                ? command.Complete()
                : command.Fail();
        }
        catch (Exception exc) {
            return command.Fail(exc);
        }
    }

    public async ValueTask<CommandResult> HandleAsync(SubscriptionMsgs.Remove command) {
        try {
            var subscription = await _repository.TryGetById<Subscription>(command.SubscriptionId);
            subscription.Remove();

            return await _repository.Save(subscription)
                ? command.Complete()
                : command.Fail();
        }
        catch (Exception exc) {
            return command.Fail(exc);
        }
    }
}