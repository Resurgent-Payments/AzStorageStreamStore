namespace LvStreamStore;

public class WrongExpectedVersionException : Exception {
    public long ExpectedVersion { get; private set; }
    public long ActualVersion { get; private set; }

    internal WrongExpectedVersionException(long expectedVersion, long actualVersion) : base() {
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}
