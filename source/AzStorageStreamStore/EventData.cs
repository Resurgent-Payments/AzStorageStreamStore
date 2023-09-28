namespace AzStorageStreamStore;

public record EventData(StreamId Key, Guid EventId, string EventType, byte[] Data);
