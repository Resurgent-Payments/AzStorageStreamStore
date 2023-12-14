namespace LvStreamStore;
public record StreamEvent(Guid? MsgId = null) : StreamMessage(MsgId);