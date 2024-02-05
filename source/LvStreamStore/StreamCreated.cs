namespace LvStreamStore;

public record StreamCreated(StreamId StreamId, long Position) : StreamMessage(StreamId, Position);