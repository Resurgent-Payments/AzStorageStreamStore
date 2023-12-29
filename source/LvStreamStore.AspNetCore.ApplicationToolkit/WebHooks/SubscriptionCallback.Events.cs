namespace LvStreamStore.ApplicationToolkit.WebHooks;

public static partial class SubscriptionCallbackMsgs {
    public record Initiated(Guid CallbackId, Uri CallbackUrl, int NumberOfRetries, object message) : Event;
    public record Completed(Guid CallbackId) : Event;
    public record Failed(Guid CallbackId, string Reason, int NumberOfRetriesLeft) : Event;
    public record RetryCallback(Guid CallbackId) : Event;
}