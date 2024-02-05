//namespace LvStreamStore.ApplicationToolkit {
//    using System;
//    using System.Threading;

//    public record Command(CancellationToken Token = default) : Message() {
//        public CommandResult Complete() => new CommandCompleted(this);
//        public CommandResult Fail(Exception exc = null) => new CommandFailed(this, exc);
//    }

//    public static class CommandResultExtensions {
//        public static T IsType<T>(this CommandResult result) where T : CommandResult {
//            return typeof(T).IsAssignableTo(result.GetType())
//                ? (T)result
//                : throw new InvalidResultException(result);
//        }
//    }

//    public class InvalidResultException : Exception {

//        public CommandResult Result { get; }

//        public InvalidResultException(CommandResult result) {
//            Result = result;
//        }
//    }
//}
