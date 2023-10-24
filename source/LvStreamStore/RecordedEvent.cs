namespace LvStreamStore;

public record RecordedEvent(StreamId StreamId, Guid EventId, long Position, string Type, byte[] Metadata, byte[] Data, Guid? MsgId = null) : StreamItem(StreamId, Position, MsgId);
