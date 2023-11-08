namespace LvStreamStore.ApplicationToolkit.WebHooks;

public partial class WebHookSubscriptionMsgs {
    public record Subscribed(Guid SubscriptionId, Guid WebHookId, string Description, string PostUrl, Guid? MsgId = null) : Event(MsgId);
    public record Disabled(Guid SubscriptionId, Guid? MsgId = null) : Event(MsgId);
    public record Enabled(Guid SubscriptionId, Guid? MsgId = null) : Event(MsgId);
    public record Removed(Guid SubscriptionId, Guid? MsgId = null) : Event(MsgId);
}