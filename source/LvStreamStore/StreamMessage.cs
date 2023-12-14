namespace LvStreamStore;
using System;

public abstract record StreamMessage {
    public Guid? MsgId { get; }

    public StreamMessage(Guid? msgId) {
        MsgId = msgId ?? Guid.NewGuid();
    }
}