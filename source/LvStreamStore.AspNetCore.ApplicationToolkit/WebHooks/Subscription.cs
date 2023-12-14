namespace LvStreamStore.ApplicationToolkit.WebHooks;

public class Subscription : AggregateRoot
{
    bool _isEnabled = false;
    bool _hasBeenRemoved = false;

    public Subscription(Guid subscriptionId, Guid webHookId, string description, string postUrl)
    {
        Ensure.NotEmptyGuid(subscriptionId, nameof(subscriptionId));
        Ensure.NotEmptyGuid(webHookId, nameof(webHookId));
        Ensure.NotNullOrEmpty(description, nameof(description));
        Ensure.NotNullOrEmpty(postUrl, nameof(postUrl));

        RegisterEvents();

        Raise(new SubscriptionMsgs.Subscribed(subscriptionId, webHookId, description, postUrl));
        Raise(new SubscriptionMsgs.Enabled(subscriptionId));
    }

    public Subscription()
    {
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        Register<SubscriptionMsgs.Subscribed>(Apply);
        Register<SubscriptionMsgs.Enabled>(Apply);
        Register<SubscriptionMsgs.Disabled>(Apply);
        Register<SubscriptionMsgs.Removed>(Apply);
    }

    public void Enable()
    {
        if (_isEnabled) return;
        Raise(new SubscriptionMsgs.Enabled(Id));
    }

    public void Disable()
    {
        if (!_isEnabled) return;
        Raise(new SubscriptionMsgs.Disabled(Id));
    }

    public void Remove()
    {
        if (_hasBeenRemoved) return;
        Raise(new SubscriptionMsgs.Removed(Id));
    }


    private void Apply(SubscriptionMsgs.Subscribed msg)
    {
        Id = msg.SubscriptionId;
    }

    private void Apply(SubscriptionMsgs.Enabled _)
    {
        _isEnabled = true;
    }

    private void Apply(SubscriptionMsgs.Disabled _)
    {
        _isEnabled = false;
    }

    private void Apply(SubscriptionMsgs.Removed _)
    {
        _isEnabled = false;
        _hasBeenRemoved = true;
    }
}