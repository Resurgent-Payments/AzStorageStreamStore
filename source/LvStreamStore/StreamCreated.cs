namespace AzStorageStreamStore;

public record StreamCreated(StreamId StreamId) : StreamItem(StreamId) { }