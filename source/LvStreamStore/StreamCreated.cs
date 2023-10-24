namespace LvStreamStore;

public record StreamCreated(StreamId StreamId, long Position, Guid? MsgId = null) : StreamItem(StreamId, Position, MsgId) { }