namespace LvStreamStore;

public abstract record StreamItem(StreamId StreamId, Guid? MsgId = null) : Event(MsgId);