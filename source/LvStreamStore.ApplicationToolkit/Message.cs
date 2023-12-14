namespace LvStreamStore.ApplicationToolkit {
    public abstract record Message(Guid? MsgId) {
        public Guid? MsgId { get; } = MsgId ?? Guid.NewGuid();
    }
}
