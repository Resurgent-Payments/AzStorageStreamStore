namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;

    public class WebHookRm : ReadModelBase,
        IAsyncHandler<SubscriptionMsgs.Subscribed>,
        IAsyncHandler<SubscriptionMsgs.Enabled>,
        IAsyncHandler<SubscriptionMsgs.Disabled>,
        IAsyncHandler<SubscriptionMsgs.Removed> {
        private readonly List<WebHookTerm> _webHooks = new();
        private readonly List<Subscription> _subscriptions = new();

        public WebHookRm(ISubscriber inBus, IStreamStoreRepository repository) : base(inBus, repository) {
            SubscribeToStream<WebHooks.Subscription, SubscriptionMsgs.Subscribed>(this);
            SubscribeToStream<WebHooks.Subscription, SubscriptionMsgs.Enabled>(this);
            SubscribeToStream<WebHooks.Subscription, SubscriptionMsgs.Disabled>(this);
            SubscribeToStream<WebHooks.Subscription, SubscriptionMsgs.Removed>(this);
        }

        public IReadOnlyList<WebHookTerm> WebHooks => _webHooks.AsReadOnly();
        public IReadOnlyList<Subscription> Subscriptions => _subscriptions.AsReadOnly();

        public ValueTask HandleAsync(SubscriptionMsgs.Subscribed msg) {
            _subscriptions.Add(new Subscription(msg.SubscriptionId, msg.WebHookId, msg.Description, msg.PostUrl) { IsEnabled = false });
            return ValueTask.CompletedTask;
        }

        public ValueTask HandleAsync(SubscriptionMsgs.Enabled msg) {
            foreach (var sub in _subscriptions.Where(s => s.SubscriptionId == msg.SubscriptionId)) {
                sub.IsEnabled = true;
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask HandleAsync(SubscriptionMsgs.Disabled msg) {
            foreach (var sub in _subscriptions.Where(s => s.SubscriptionId == msg.SubscriptionId)) {
                sub.IsEnabled = false;
            }
            return ValueTask.CompletedTask;
        }

        public ValueTask HandleAsync(SubscriptionMsgs.Removed msg) {
            _subscriptions.RemoveAll(sub => sub.SubscriptionId == msg.SubscriptionId);
            return ValueTask.CompletedTask;
        }

        public void RegisterMessage<T>() {
            var attr = typeof(T).GetCustomAttribute<WebHookMessageAttribute>();
            if (attr == null) { return; }
            _webHooks.Add(new WebHookTerm { Id = attr.WebHookId, Name = attr.Name, Description = attr.Description });
        }

        public void RegisterMessage(Type t) {
            var attr = t.GetCustomAttribute<WebHookMessageAttribute>();
            if (attr == null) { return; }
            _webHooks.Add(new WebHookTerm { Id = attr.WebHookId, Name = attr.Name, Description = attr.Description });
        }

        public class WebHookTerm {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        public class Subscription {
            public readonly Guid SubscriptionId;
            public readonly Guid WebHookId;
            public readonly string Description = string.Empty;
            public readonly string PostUrl = string.Empty;
            public bool IsEnabled;

            public Subscription(Guid subscriptionId, Guid webHookId, string description, string postUrl) {
                SubscriptionId = subscriptionId;
                WebHookId = webHookId;
                Description = description;
                PostUrl = postUrl;
            }
        }
    }
}
