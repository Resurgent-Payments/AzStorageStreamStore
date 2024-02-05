namespace LvStreamStore;

public abstract record StreamMessage(StreamId StreamId, long Position) : Messaging.Message;