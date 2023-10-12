namespace LvStreamStore {
    using System;

    public interface IMessage {
        Guid MsgId { get; }
    }

}
