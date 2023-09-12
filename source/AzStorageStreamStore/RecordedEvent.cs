namespace AzStorageStreamStore;

public record RecordedEvent(StreamId StreamId, Guid EventId, long Revision, byte[] Data) : StreamItem(StreamId);
