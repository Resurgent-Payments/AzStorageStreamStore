namespace LvStreamStore;
using System;

public abstract record StreamMessage : Messaging.Message {
    public Guid? MsgId { get; }

    public StreamMessage(Guid? msgId) {
        MsgId = msgId ?? Guid.NewGuid();
    }
}