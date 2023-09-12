namespace AzStorageStreamStore;

public record WriteResult(bool Successful, long Position, long Version, Exception Exception = default) {
    public static WriteResult Ok(long position, long version) => new WriteResult(true, version, position);
    public static WriteResult Failed(long position, long version, Exception exc) => new WriteResult(false, position, version, exc);
}
