namespace LvStreamStore.ApplicationToolkit {
    public record Event(Guid? MsgId = null) : Message(MsgId);
}
