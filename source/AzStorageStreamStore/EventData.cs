namespace AzStorageStreamStore;

public record EventData(StreamId Key, Guid EventId, byte[] Data);
