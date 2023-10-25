namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading;

    public record Command(CancellationToken Token = default, Guid? MsgId = null) : Message(MsgId) {
        public CommandResult Complete(Guid? MsgId = null) => new CommandCompleted(this, MsgId);
        public CommandResult Fail(Exception exc = null, Guid? MsgId = null) => new CommandFailed(this, exc, MsgId);
    }
}
