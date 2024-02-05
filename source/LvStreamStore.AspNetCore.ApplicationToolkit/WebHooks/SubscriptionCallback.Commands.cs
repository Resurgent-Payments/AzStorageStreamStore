namespace LvStreamStore.ApplicationToolkit.WebHooks;

using LvStreamStore.Messaging;

public static partial class SubscriptionCallbackMsgs {
    public record SendMessageToSubscriber(Guid CallbackId, Uri CallbackUri, object Message, CancellationToken Token = default) : Message;
}