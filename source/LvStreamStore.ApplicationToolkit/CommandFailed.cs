namespace LvStreamStore.ApplicationToolkit {
    using System;

    public record CommandFailed(Command SourceCommand, Exception Exception = null, Guid? MsgId = null) : CommandResult(SourceCommand, MsgId);
}
