namespace LvStreamStore;
public record CommandResult(StreamCommand command) : StreamMessage(command.MsgId) { }