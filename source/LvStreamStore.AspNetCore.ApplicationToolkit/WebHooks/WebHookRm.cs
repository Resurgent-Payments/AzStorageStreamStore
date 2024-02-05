namespace LvStreamStore.ApplicationToolkit.WebHooks {
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;

    using LvStreamStore.Messaging;

    public class WebHookRm : ReadModelBase,
        IReceiver<SubscriptionMsgs.Subscribed>,
        IReceiver<SubscriptionMsgs.Enabled>,
        IReceiver<SubscriptionMsgs.Disabled>,
        IReceiver<SubscriptionMsgs.Removed> {
        private readonly List<WebHookTerm> _webHooks = new();
        private readonly List<Subscription> _subscriptions = new();

        public WebHookRm(AsyncDispatcher dispatcher, IStreamStoreRepository repository) : base(dispatcher, repository) {
            SubscribeToStream<SubscriptionMsgs.Subscribed>(this);
            SubscribeToStream<SubscriptionMsgs.Enabled>(this);
            SubscribeToStream<SubscriptionMsgs.Disabled>(this);
            SubscribeToStream<SubscriptionMsgs.Removed>(this);
        }

        public IReadOnlyList<WebHookTerm> WebHooks => _webHooks.AsReadOnly();
        public IReadOnlyList<Subscription> Subscriptions => _subscriptions.AsReadOnly();

        public Task Receive(SubscriptionMsgs.Subscribed msg) {
            _subscriptions.Add(new Subscription(msg.SubscriptionId, msg.WebHookId, msg.Description, msg.PostUrl) { IsEnabled = false });
            return Task.CompletedTask;
        }

        public Task Receive(SubscriptionMsgs.Enabled msg) {
            foreach (var sub in _subscriptions.Where(s => s.SubscriptionId == msg.SubscriptionId)) {
                sub.IsEnabled = true;
            }
            return Task.CompletedTask;
        }

        public Task Receive(SubscriptionMsgs.Disabled msg) {
            foreach (var sub in _subscriptions.Where(s => s.SubscriptionId == msg.SubscriptionId)) {
                sub.IsEnabled = false;
            }
            return Task.CompletedTask;
        }

        public Task Receive(SubscriptionMsgs.Removed msg) {
            _subscriptions.RemoveAll(sub => sub.SubscriptionId == msg.SubscriptionId);
            return Task.CompletedTask;
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
