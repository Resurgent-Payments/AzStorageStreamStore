namespace LvStreamStore.ApplicationToolkit.WebHooks;

public partial class SubscriptionMsgs
{
    public record Subscribe(Guid SubscriptionId, Guid WebHookId, string Description, string PostUrl, CancellationToken Token = default) : Command(Token);

    public record Enable(Guid SubscriptionId, CancellationToken Token = default) : Command(Token);

    public record Disable(Guid SubscriptionId, CancellationToken Token = default) : Command(Token);

    public record Remove(Guid SubscriptionId, CancellationToken Token = default) : Command(Token);
}