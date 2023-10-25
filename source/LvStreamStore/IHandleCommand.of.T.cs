namespace LvStreamStore;
using System.Threading.Tasks;

internal interface IHandleCommand<T> where T : Command {
    Task<CommandResult> HandleAsync(T cmd);
}
