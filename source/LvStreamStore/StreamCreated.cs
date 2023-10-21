namespace LvStreamStore;

public record StreamCreated(StreamId StreamId, Guid? MsgId = null) : StreamItem(StreamId, MsgId) { }