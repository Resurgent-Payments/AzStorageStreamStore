using System.Collections;

/// <summary>
/// Akin to a $ce- in event store, this can express the physical stream through all categories that may exist through a classification hierarchy.
/// </summary>
/// <param name="Categories"></param>
public record StreamKey(string[] Categories) : IEnumerable<StreamKey> {
    public static bool operator ==(StreamKey key, StreamId id) => id.Equals(key);
    public static bool operator !=(StreamKey key, StreamId id) => !id.Equals(key);

    public override int GetHashCode() {
        var hash = new HashCode();
        foreach (var cat in Categories) {
            hash.Add(cat.GetHashCode());
        }
        return hash.ToHashCode();
    }

    public virtual bool Equals(StreamKey? other) {
        if (ReferenceEquals(null, other))
            return false;

        if (Categories.Length > other.Categories.Length) return false;

        for (int i = 0; i < Categories.Length; i++) {
            if (!Categories[i].Equals(other.Categories[i]))
                return false;
        }

        return true;
    }

    public IEnumerator<StreamKey> GetEnumerator() => new Enumerator(Categories);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class Enumerator : IEnumerator<StreamKey> {
        private int _index = 0;
        string[] _allKeys;

        public Enumerator(IEnumerable<string> allKeys) {
            if (allKeys.Count() < 1) throw new ArgumentException("Cannot enumerate.  Need at least one key.");
            _allKeys = allKeys.ToArray();
        }

        public StreamKey Current { get; private set; }

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            if (_index >= _allKeys.Length) return false;

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
