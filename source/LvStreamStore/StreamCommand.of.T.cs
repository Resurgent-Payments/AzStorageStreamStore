//namespace LvStreamStore;
//public record StreamCommand(Guid? MsgId = null) : StreamMessage(Guid.Empty) {
//    public CommandResult Ok() => new Ok(this);
//    public CommandResult Fail(Exception? exception) => new Fail(this, exception);
//}