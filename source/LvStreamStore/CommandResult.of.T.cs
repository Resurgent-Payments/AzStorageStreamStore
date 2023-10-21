namespace LvStreamStore;
public record CommandResult(Command command) : Message(command.MsgId) { }