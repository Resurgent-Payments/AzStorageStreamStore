namespace LvStreamStore;
public record Ok(Command command) : CommandResult(command) { }