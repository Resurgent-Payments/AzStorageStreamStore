namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading;

    public record Command(CancellationToken Token = default, Guid? MsgId = null) : Message(MsgId) {
        public CommandResult Complete(Guid? MsgId = null) => new CommandCompleted(this, MsgId);
        public CommandResult Fail(Exception exc = null, Guid? MsgId = null) => new CommandFailed(this, exc, MsgId);
    }

    public static class CommandResultExtensions {
        public static T IsType<T>(this CommandResult result) where T : CommandResult {
            return typeof(T).IsAssignableTo(result.GetType())
                ? (T)result
                : throw new InvalidResultException(result);
        }
    }

    public class InvalidResultException : Exception {

        public CommandResult Result { get; }

        public InvalidResultException(CommandResult result) {
            Result = result;
        }
    }
}
