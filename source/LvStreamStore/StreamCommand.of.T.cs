namespace LvStreamStore;
public record Command(Guid? MsgId = null) : Message(Guid.Empty) {
    public CommandResult Ok() => new Ok(this);
    public CommandResult Fail(Exception? exception) => new Fail(this, exception);
}