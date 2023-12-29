namespace LvStreamStore.ApplicationToolkit {
    public record CommandCompleted(Command SourceCommand) : CommandResult(SourceCommand);
}
