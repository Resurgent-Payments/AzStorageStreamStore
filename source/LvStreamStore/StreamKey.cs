namespace LvStreamStore;

using System.Collections;

/// <summary>
/// Akin to a $ce- in event store, this can express the physical stream through all categories that may exist through a classification hierarchy.
/// </summary>
/// <param name="Categories"></param>
public record StreamKey(string[] Categories) : IEnumerable<StreamKey> {
    public static StreamKey All = new StreamKey(Array.Empty<string>());
    public static implicit operator StreamId(StreamKey key) => new(key.Categories.First(), key.Categories.Skip(1).Take(key.Categories.Length - 2).ToArray(), key.Categories.Last());
    public static bool operator ==(StreamKey key, StreamId id) => id == key;
    public static bool operator !=(StreamKey key, StreamId id) => !(key == id);

    public override int GetHashCode() {
        var hash = new HashCode();
        foreach (var cat in Categories) {
            hash.Add(cat.GetHashCode());
        }
        return hash.ToHashCode();
    }

    public virtual bool Equals(StreamKey? other) {
        if (other is null)
            return false;

        if (Categories.Length > other.Categories.Length) return false;

        for (var i = 0; i < Categories.Length; i++) {
            if (!Categories[i].Equals(other.Categories[i]))
                return false;
        }

        return true;
    }

    public IEnumerator<StreamKey> GetEnumerator() => new Enumerator(Categories);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class Enumerator : IEnumerator<StreamKey> {
        private int _index = 0;
        private readonly string[] _allKeys;

        public Enumerator(IEnumerable<string> allKeys) {
            if (!allKeys.Any()) throw new ArgumentException("Cannot enumerate.  Need at least one key.");
            _allKeys = allKeys.ToArray();
        }

        public StreamKey Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            if (_index > _allKeys.Length) return false;

            _index += 1;
            Current = new StreamKey(_allKeys.Take(_index).ToArray());
            return true;
        }

        public void Reset() {
            Current = null;
            _index = 0;
        }

        public void Dispose() {
            // no-op
        }
    }
}