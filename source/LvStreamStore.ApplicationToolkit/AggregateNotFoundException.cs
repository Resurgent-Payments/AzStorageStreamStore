namespace LvStreamStore.ApplicationToolkit {
    using System;

    public class AggregateNotFoundException<TAggregate> : Exception {
        public Type AggregateType => typeof(TAggregate);
    }
}
