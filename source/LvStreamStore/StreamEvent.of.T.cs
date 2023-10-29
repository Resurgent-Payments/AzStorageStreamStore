namespace LvStreamStore;
public record StreamEvent(Guid? MsgId = null) : Message(MsgId);