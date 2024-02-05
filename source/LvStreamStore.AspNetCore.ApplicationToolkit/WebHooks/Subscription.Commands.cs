namespace LvStreamStore.ApplicationToolkit.WebHooks;

using LvStreamStore.Messaging;

public partial class SubscriptionMsgs
{
    public record Subscribe(Guid SubscriptionId, Guid WebHookId, string Description, string PostUrl, CancellationToken Token = default) : Message;

    public record Enable(Guid SubscriptionId, CancellationToken Token = default) : Message;

    public record Disable(Guid SubscriptionId, CancellationToken Token = default) : Message;

    public record Remove(Guid SubscriptionId, CancellationToken Token = default) : Message;
}