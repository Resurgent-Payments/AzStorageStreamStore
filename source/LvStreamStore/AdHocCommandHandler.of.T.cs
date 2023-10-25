namespace LvStreamStore;

internal class AdHocCommandHandler<T> : IHandleCommand<T> where T : Command {
    private readonly Func<T, Task<CommandResult>> _handle;

    public AdHocCommandHandler(Func<T, Task<CommandResult>> handle) {
        _handle = handle;
    }

    public Task<CommandResult> HandleAsync(T cmd) => _handle.Invoke(cmd);
}
