namespace AzStorageStreamStore;

public record RecordedEvent(StreamId Key, Guid EventId, long Position, byte[] Data);
