namespace LvStreamStore.ApplicationToolkit.WebHooks;

public static partial class SubscriptionCallbackMsgs {
    public record SendMessageToSubscriber(Guid CallbackId, Uri CallbackUri, object Message, CancellationToken Token = default, Guid? MsgId = null) : Command(Token, MsgId);
}