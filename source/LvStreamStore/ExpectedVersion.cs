namespace LvStreamStore;
public class ExpectedVersion {
    private long _value;

    /// <summary>
    /// This write should not conflict with anything and should always succeed.
    /// </summary>
    public static readonly ExpectedVersion Any = new(-2);
    /// <summary>
    /// The stream being written to should not yet exist. If it does exist treat that as a concurrency problem.
    /// </summary>
    public static readonly ExpectedVersion NoStream = new(-1);
    /// <summary>
    /// The stream should exist and should be empty. If it does not exist or is not empty treat that as a concurrency problem.
    /// </summary>
    public static readonly ExpectedVersion EmptyStream = new(-1);
    /// <summary>
    /// The stream should exist. If it or a metadata stream does not exist treat that as a concurrency problem.
    /// </summary>
    public static readonly ExpectedVersion StreamExists = new(-4);

    public ExpectedVersion(long version) {
        _value = version;
    }


    public static implicit operator long(ExpectedVersion v) => v._value;
    public static implicit operator ExpectedVersion(long value) => new(value);
    public override string ToString() => $"ExpectedVersion: {_value}";
    public override int GetHashCode() => _value.GetHashCode();
}
