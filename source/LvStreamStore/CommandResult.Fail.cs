namespace LvStreamStore;
public record Fail(Command command, Exception? Exception = default) : CommandResult(command) { }
