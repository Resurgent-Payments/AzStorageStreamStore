namespace LvStreamStore;

public class WrongExpectedVersionException : Exception {
    public long ExpectedVersion { get; private set; }
    public long ActualVersion { get; private set; }

    public WrongExpectedVersionException(long expectedVersion, long actualVersion) {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
