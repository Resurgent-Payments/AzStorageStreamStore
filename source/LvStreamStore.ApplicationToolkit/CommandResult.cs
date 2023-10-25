namespace LvStreamStore.ApplicationToolkit {
    public record CommandResult(Command SourceCommand, Guid? MsgId) : Message(MsgId);
}
