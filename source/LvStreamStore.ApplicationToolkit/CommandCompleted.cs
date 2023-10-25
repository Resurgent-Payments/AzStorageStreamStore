namespace LvStreamStore.ApplicationToolkit {
    public record CommandCompleted(Command SourceCommand, Guid? MsgId = null) : CommandResult(SourceCommand, MsgId);
}
