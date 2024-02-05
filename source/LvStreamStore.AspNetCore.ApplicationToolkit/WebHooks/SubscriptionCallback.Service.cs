namespace LvStreamStore.ApplicationToolkit.WebHooks;

using System.Threading.Tasks;

using LvStreamStore.Messaging;

using Microsoft.Extensions.Options;

public class SubscriptionCallbackService :
    TransientSubscriber,

    IHandleAsync<SubscriptionCallbackMsgs.SendMessageToSubscriber>,
    IReceiver<SubscriptionCallbackMsgs.Failed> {
    private readonly IStreamStoreRepository _streamStoreRepository;
    private SubscriptionCallbackServiceOptions _options;

    public SubscriptionCallbackService(AsyncDispatcher outBus, IStreamStoreRepository streamStoreRepository, IOptions<SubscriptionCallbackServiceOptions> serviceOptions)
        : base(outBus) {
        _streamStoreRepository = streamStoreRepository;
        _options = serviceOptions.Value!;

        Subscribe<SubscriptionCallbackMsgs.SendMessageToSubscriber>(this);
        Subscribe<SubscriptionCallbackMsgs.Failed>(this);
    }

    public async Task HandleAsync(SubscriptionCallbackMsgs.SendMessageToSubscriber command) {
        //todo: add configurable number of retries.
        var callback = new SubscriptionCallback(command.CallbackId, command.CallbackUri, _options.MaximumNumberOfRetries, command.Message);
        await _streamStoreRepository.Save(callback);
    }

    //TODO: Implement after MVP works.
    public Task Receive(SubscriptionCallbackMsgs.Failed @event) {
        //if (@event.NumberOfRetriesLeft > 0) {
        //    var envelope = new DelaySendEnvelope(null, new SubscriptionCallbackMsgs.RetryCallback(@event.CallbackId));
        //    await _outBus.PublishAsync(envelope);
        //}
        return Task.CompletedTask;
    }
}