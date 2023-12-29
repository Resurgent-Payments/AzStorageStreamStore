namespace LvStreamStore.ApplicationToolkit.WebHooks;

public partial class SubscriptionMsgs {
    public record Subscribed(Guid SubscriptionId, Guid WebHookId, string Description, string PostUrl) : Event;
    public record Disabled(Guid SubscriptionId) : Event;
    public record Enabled(Guid SubscriptionId) : Event;
    public record Removed(Guid SubscriptionId) : Event;
}