namespace AzStorageStreamStore;

public class ExpectedVersion {
    public long Value { get; set; }

    public static ExpectedVersion EmptyStream = -1;
    public static ExpectedVersion Any = -2;
    public static ExpectedVersion NoStream = -3;


    public static implicit operator long(ExpectedVersion v) => v.Value;
    public static implicit operator ExpectedVersion(long value) => new() { Value = value };
    public override string ToString() => Value.ToString();
    public override int GetHashCode() => Value.GetHashCode();
}
