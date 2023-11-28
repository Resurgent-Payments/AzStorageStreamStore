namespace LvStreamStore.ApplicationToolkit.WebHooks;

public static partial class SubscriptionCallbackMsgs {
    public record Initiated(Guid CallbackId, Uri CallbackUrl, int NumberOfRetries, object message, Guid? MsgId = null) : Event(MsgId);
    public record Completed(Guid CallbackId, Guid? MsgId = null) : Event(MsgId);
    public record Failed(Guid CallbackId, string Reason, int NumberOfRetriesLeft, Guid? MsgId = null) : Event(MsgId);
    public record RetryCallback(Guid CallbackId, Guid? MsgId = null) : Event(MsgId);
}