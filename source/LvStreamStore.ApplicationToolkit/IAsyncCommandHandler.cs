namespace LvStreamStore.ApplicationToolkit {
    using System.Threading.Tasks;

    public interface IAsyncCommandHandler<TCommand> where TCommand : Command {
        ValueTask<CommandResult> HandleAsync(TCommand command);
    }
}
