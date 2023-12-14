namespace LvStreamStore.Subscriptions {
    using System;

    internal class EventStreamPollerOptions {
        public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMilliseconds(100);
    }
}
