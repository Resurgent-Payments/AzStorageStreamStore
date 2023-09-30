namespace LvStreamStore;

public record StreamCreated(StreamId StreamId) : StreamItem(StreamId) { }