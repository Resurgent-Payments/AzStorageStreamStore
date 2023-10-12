namespace LvStreamStore {
    using System;

    public class EventRecorded : IMessage {
        public Guid MsgId { get; private set; }
        public RecordedEvent Event { get; private set; }

        public EventRecorded(RecordedEvent @event) {
            MsgId = Guid.NewGuid();
            Event = @event;
        }
    }
}
