namespace AzStorageStreamStore;

public record RecordedEvent(StreamId StreamId, Guid EventId, long Revision, string Type, byte[] Data) : StreamItem(StreamId);
