namespace LvStreamStore.ApplicationToolkit.WebHooks;

public class SubscriptionCallback : AggregateRoot {
    private int _numberOfRetries = 0;

    public bool HasCompleted { get; private set; } = false;

    public SubscriptionCallback(Guid callbackId, Uri callbackUrl, int numberOfRetries, object message) {
        RegisterEvents();
        Raise(new SubscriptionCallbackMsgs.Initiated(callbackId, callbackUrl, numberOfRetries, message));
    }

    public SubscriptionCallback() {
        RegisterEvents();
    }

    private void RegisterEvents() {
        Register<SubscriptionCallbackMsgs.Initiated>(Apply);
        Register<SubscriptionCallbackMsgs.Completed>(Apply);
        Register<SubscriptionCallbackMsgs.Failed>(Apply);
    }

    public void Complete() {
        if (HasCompleted) { return; }
        Raise(new SubscriptionCallbackMsgs.Completed(Id));
    }

    public void Fail(string reason) {
        if (HasCompleted) { return; }
        var retriesLeft = _numberOfRetries - 1;
        Raise(new SubscriptionCallbackMsgs.Failed(Id, reason, retriesLeft));
        if (_numberOfRetries <= 0) { Raise(new SubscriptionCallbackMsgs.Completed(Id)); }
    }

    private void Apply(SubscriptionCallbackMsgs.Initiated msg) {
        Id = msg.CallbackId;
        _numberOfRetries = msg.NumberOfRetries;
    }

    private void Apply(SubscriptionCallbackMsgs.Completed msg) {
        HasCompleted = true;
    }

    public void Apply(SubscriptionCallbackMsgs.Failed msg) {
        _numberOfRetries = msg.NumberOfRetriesLeft;
    }
}