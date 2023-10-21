namespace LvStreamStore;

public record RecordedEvent(StreamId StreamId, Guid EventId, long Revision, string Type, byte[] Metadata, byte[] Data, Guid? MsgId = null) : StreamItem(StreamId, MsgId);
