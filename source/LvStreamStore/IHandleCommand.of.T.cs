namespace LvStreamStore;
using System.Threading.Tasks;

public interface IHandleCommand<T> where T : Command {
    Task<CommandResult> HandleAsync(T cmd);
}
