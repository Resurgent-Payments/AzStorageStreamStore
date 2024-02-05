namespace LvStreamStore.Messaging;

public abstract record Message {
    public Guid? MsgId { get; set; }

    public Message() {
        MsgId = MsgId ?? Guid.NewGuid();
    }
}
