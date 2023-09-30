namespace LvStreamStore;
public record WriteResult(bool Successful, long Version, Exception Exception = default) {
    public static WriteResult Ok(long version) => new WriteResult(true, version);
    public static WriteResult Failed(long version, Exception exc) => new WriteResult(false, version, exc);
}
