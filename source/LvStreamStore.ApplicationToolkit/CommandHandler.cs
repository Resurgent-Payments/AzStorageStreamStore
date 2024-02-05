//namespace LvStreamStore.ApplicationToolkit {
//    using System.Threading.Tasks;

//    public class AsyncAdHocCommandHandler<TCommand> : IAsyncCommandHandler<TCommand> where TCommand : Command {
//        private readonly Func<TCommand, ValueTask<CommandResult>> _handles;

//        public AsyncAdHocCommandHandler(Func<TCommand, ValueTask<CommandResult>> handles) {
//            _handles = handles;
//        }

//        public ValueTask<CommandResult> HandleAsync(TCommand command) => _handles.Invoke(command);
//    }
//}
