namespace LvStreamStore.ApplicationToolkit.WebHooks;

public class WebHookSubscription : AggregateRoot
{
    bool _isEnabled = false;
    bool _hasBeenRemoved = false;

    public WebHookSubscription(Guid subscriptionId, Guid webHookId, string description, string postUrl)
    {
        Ensure.NotEmptyGuid(subscriptionId, nameof(subscriptionId));
        Ensure.NotEmptyGuid(webHookId, nameof(webHookId));
        Ensure.NotNullOrEmpty(description, nameof(description));
        Ensure.NotNullOrEmpty(postUrl, nameof(postUrl));

        RegisterEvents();

        Raise(new WebHookSubscriptionMsgs.Subscribed(subscriptionId, webHookId, description, postUrl));
        Raise(new WebHookSubscriptionMsgs.Enabled(subscriptionId));
    }

    public WebHookSubscription()
    {
        RegisterEvents();
    }

    private void RegisterEvents()
    {
        Register<WebHookSubscriptionMsgs.Subscribed>(Apply);
        Register<WebHookSubscriptionMsgs.Enabled>(Apply);
        Register<WebHookSubscriptionMsgs.Disabled>(Apply);
        Register<WebHookSubscriptionMsgs.Removed>(Apply);
    }

    public void Enable()
    {
        if (_isEnabled) return;
        Raise(new WebHookSubscriptionMsgs.Enabled(Id));
    }

    public void Disable()
    {
        if (!_isEnabled) return;
        Raise(new WebHookSubscriptionMsgs.Disabled(Id));
    }

    public void Remove()
    {
        if (_hasBeenRemoved) return;
        Raise(new WebHookSubscriptionMsgs.Removed(Id));
    }


    private void Apply(WebHookSubscriptionMsgs.Subscribed msg)
    {
        Id = msg.SubscriptionId;
    }

    private void Apply(WebHookSubscriptionMsgs.Enabled _)
    {
        _isEnabled = true;
    }

    private void Apply(WebHookSubscriptionMsgs.Disabled _)
    {
        _isEnabled = false;
    }

    private void Apply(WebHookSubscriptionMsgs.Removed _)
    {
        _isEnabled = false;
        _hasBeenRemoved = true;
    }
}