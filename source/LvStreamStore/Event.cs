namespace LvStreamStore;
public record Event(Guid? MsgId = null) : Message(MsgId);