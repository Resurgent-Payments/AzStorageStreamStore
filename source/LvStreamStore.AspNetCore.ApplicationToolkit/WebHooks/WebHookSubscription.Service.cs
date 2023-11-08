namespace LvStreamStore.ApplicationToolkit.WebHooks;

//todo: need to make this internal and use it as part of a fluent configuration.
public class WebHookSubscriptionService :
    IAutoStartService, // service locator interface.
    IAsyncCommandHandler<WebHookSubscriptionMsgs.Subscribe>,
    IAsyncCommandHandler<WebHookSubscriptionMsgs.Enable>,
    IAsyncCommandHandler<WebHookSubscriptionMsgs.Disable>,
    IAsyncCommandHandler<WebHookSubscriptionMsgs.Remove> {
    IStreamStoreRepository _repository;

    public WebHookSubscriptionService(IStreamStoreRepository repository, IDispatcher dispatcher) {
        _repository = repository;
        dispatcher.Subscribe<WebHookSubscriptionMsgs.Subscribe>(this);
        dispatcher.Subscribe<WebHookSubscriptionMsgs.Enable>(this);
        dispatcher.Subscribe<WebHookSubscriptionMsgs.Disable>(this);
        dispatcher.Subscribe<WebHookSubscriptionMsgs.Remove>(this);
    }

    public async ValueTask<CommandResult> HandleAsync(WebHookSubscriptionMsgs.Subscribe command) {
        try {
            return await _repository.Save(new WebHookSubscription(command.SubscriptionId, command.WebHookId, command.Description, command.PostUrl))
                ? command.Complete()
                : command.Fail();
        }
        catch (Exception ex) {
            return command.Fail(ex);
        }
    }

    public async ValueTask<CommandResult> HandleAsync(WebHookSubscriptionMsgs.Enable command) {
        try {
            var subscription = await _repository.TryGetById<WebHookSubscription>(command.SubscriptionId);
            subscription.Enable();

            return await _repository.Save(subscription)
                ? command.Complete()
                : command.Fail();
        }
        catch (Exception exc) {
            return command.Fail(exc);
        }
    }

    public async ValueTask<CommandResult> HandleAsync(WebHookSubscriptionMsgs.Disable command) {
        try {
            var subscription = await _repository.TryGetById<WebHookSubscription>(command.SubscriptionId);
            subscription.Disable();

            return await _repository.Save(subscription)
                ? command.Complete()
                : command.Fail();
        }
        catch (Exception exc) {
            return command.Fail(exc);
        }
    }

    public async ValueTask<CommandResult> HandleAsync(WebHookSubscriptionMsgs.Remove command) {
        try {
            var subscription = await _repository.TryGetById<WebHookSubscription>(command.SubscriptionId);
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