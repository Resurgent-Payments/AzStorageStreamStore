namespace LvStreamStore;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Text;

public interface IStreamIndexer {
    ValueTask InitializeAsync(EventStreamReader reader);
    void Index(StreamId streamId, long position, int offset);
    void Index<TKey>(TKey key, long position, int offset);
    void Index(byte[] key, long position, int offset);
    bool GetHashTable<TKey>(TKey key, out HashSet<(long Position, int Offset)> table);
    bool GetHashTable(byte[] key, out HashSet<(long Position, int Offset)> table);
}

public class NoIndexer : IStreamIndexer {
    public ValueTask InitializeAsync(EventStreamReader reader) => ValueTask.CompletedTask;

    public bool GetHashTable<TKey>(TKey key, out HashSet<(long Position, int Offset)> table) {
        table = new();
        return false;
    }

    public bool GetHashTable(byte[] key, out HashSet<(long Position, int Offset)> table) {
        table = new();
        return false;
    }

    public void Index(StreamId streamId, long position, int offset) {

    }

    public void Index<TKey>(TKey key, long position, int offset) {

    }

    public void Index(byte[] key, long position, int offset) {

    }
}

public class MemoryStreamIndexer : IStreamIndexer {
    private readonly Dictionary<byte[], HashSet<(long Position, int Offset)>> _table = new(new ByteArrayComparer());

    public MemoryStreamIndexer() {

    }

    public async ValueTask InitializeAsync(EventStreamReader reader) {
        await foreach (var item in reader) {
            //Index(item.Current, item.Position, item.Offset);
        }
    }

    public bool GetHashTable<TKey>(TKey key, out HashSet<(long Position, int Offset)> table) {
        return GetHashTable(XxHash32.Hash(Encoding.UTF8.GetBytes(key!.ToString()!)), out table);
    }

    public bool GetHashTable(byte[] key, out HashSet<(long Position, int Offset)> table) {
        if (!_table.TryGetValue(key, out var tbl)) {
            table = new();
            return false;
        }

        table = tbl;
        return true;
    }

    public void Index(StreamId streamId, long position, int offset) {
        // need to index the stream itself
        Index(XxHash32.Hash(Encoding.UTF8.GetBytes(streamId!.ToString()!)), position, offset);

        // need to get the "key" from the stream id.
        var key = (StreamKey)streamId;
        foreach (var k in key.GetAncestors()) {
            Index(XxHash32.Hash(Encoding.UTF8.GetBytes(k!.ToString()!)), position, offset);
        }
    }

    public void Index<TKey>(TKey key, long position, int offset) {
        Index(XxHash32.Hash(Encoding.UTF8.GetBytes(key!.ToString()!)), position, offset);
    }

    public void Index(byte[] key, long position, int offset) {
        if (!_table.TryGetValue(key, out var table)) {
            table = new();
            _table.Add(key, table);
        }
        table.Add((position, offset));
    }
}

class ByteArrayComparer : IEqualityComparer<byte[]> {
    public bool Equals(byte[]? x, byte[]? y) {
        if (x == null || y == null) {
            // null == null returns true.
            // non-null == null returns false.
            return x == y;
        }
        if (ReferenceEquals(x, y)) {
            return true;
        }
        if (x.Length != y.Length) {
            return false;
        }
        // Linq extension method is based on IEnumerable, must evaluate every item.
        return x.SequenceEqual(y);
    }

    public int GetHashCode([DisallowNull] byte[] obj) {
        if (obj == null) {
            throw new ArgumentNullException("obj");
        }
        if (obj.Length >= 4) {
            return BitConverter.ToInt32(obj, 0);
        }
        // Length occupies at most 2 bits. Might as well store them in the high order byte
        int value = obj.Length;
        foreach (var b in obj) {
            value <<= 8;
            value += b;
        }
        return value;
    }
}