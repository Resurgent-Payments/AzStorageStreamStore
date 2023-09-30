namespace LvStreamStore;

public record EventData(StreamId Key, Guid EventId, string Type, byte[] Metadata, byte[] Data);
