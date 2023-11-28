namespace LvStreamStore.ApplicationToolkit.WebHooks;

using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class SubscriptionCallbackService : IAutoStartService,
    IAsyncCommandHandler<SubscriptionCallbackMsgs.SendMessageToSubscriber>,
    IAsyncHandler<SubscriptionCallbackMsgs.Failed> {
    private readonly IPublisher _outBus;
    private readonly IStreamStoreRepository _streamStoreRepository;
    private SubscriptionCallbackServiceOptions _options;

    public SubscriptionCallbackService(IPublisher outBus, IStreamStoreRepository streamStoreRepository, IOptions<SubscriptionCallbackServiceOptions> serviceOptions, IDispatcher dispatcher, ILoggerFactory factory) {
        _outBus = outBus;
        _streamStoreRepository = streamStoreRepository;
        _options = serviceOptions.Value!;

        dispatcher.Subscribe<SubscriptionCallbackMsgs.SendMessageToSubscriber>(this);
        dispatcher.Subscribe<SubscriptionCallbackMsgs.Failed>(this);
        //TODO: Research if this needs to be done instead of ^^^^
        //streamStoreRepository.Subscribe<SubscriptionCallback, SubscriptionCallbackMsgs.Failed>(this);
    }

    public async ValueTask<CommandResult> HandleAsync(SubscriptionCallbackMsgs.SendMessageToSubscriber command) {
        //todo: add configurable number of retries.
        var callback = new SubscriptionCallback(command.CallbackId, command.CallbackUri, _options.MaximumNumberOfRetries, command.Message);
        await _streamStoreRepository.Save(callback);
        return command.Complete();
    }

    //TODO: Implement after MVP works.
    public ValueTask HandleAsync(SubscriptionCallbackMsgs.Failed @event) {
        //if (@event.NumberOfRetriesLeft > 0) {
        //    var envelope = new DelaySendEnvelope(null, new SubscriptionCallbackMsgs.RetryCallback(@event.CallbackId));
        //    await _outBus.PublishAsync(envelope);
        //}
        return ValueTask.CompletedTask;
    }
}