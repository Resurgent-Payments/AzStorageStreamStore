namespace LvStreamStore;
public record CommandResult(StreamCommand command) : Message(command.MsgId) { }