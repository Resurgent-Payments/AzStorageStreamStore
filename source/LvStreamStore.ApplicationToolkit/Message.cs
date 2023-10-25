namespace LvStreamStore.ApplicationToolkit {
    public abstract record Message {
        public Guid? MsgId { get; }

        public Message(Guid? msgId) {
            MsgId = msgId ?? Guid.NewGuid();
        }
    };
}
