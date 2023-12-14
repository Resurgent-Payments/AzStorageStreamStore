namespace LvStreamStore;

public abstract record StreamItem(StreamId StreamId, long Position, Guid? MsgId = null) : StreamEvent(MsgId);