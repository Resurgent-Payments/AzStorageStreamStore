namespace LvStreamStore;

public class StreamKey {
    public static StreamKey All = new StreamKey("*");
    public static StreamKey NULL = new StreamKey();

    private readonly string[] _categories;

    public StreamKey(params string[] categories) {
        _categories = categories;
    }

    public static bool operator ==(StreamKey key, StreamId id) => key == (StreamKey)id;
    public static bool operator !=(StreamKey key, StreamId id) => !(key == (StreamKey)id);
    public static bool operator ==(StreamKey key1, StreamKey key2) => key1?.Equals(key2) ?? false;
    public static bool operator !=(StreamKey key1, StreamKey key2) => !(key1?.Equals(key2)) ?? true;


    /// <summary>
    /// Gets all parents of this key, starting with the top entry
    /// </summary>
    /// <returns></returns>
    public IEnumerable<StreamKey> GetAncestors() {
        for (var i = 1; i < _categories.Length; i++) {
            yield return new StreamKey(_categories.Take(i).ToArray());
        }
    }


    public override int GetHashCode() {
        var hash = new HashCode();
        foreach (var cat in _categories) {
            hash.Add(cat.GetHashCode());
        }

        return hash.ToHashCode();
    }

    public override bool Equals(object? obj) {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;

        if (obj is StreamKey other) {
            if (_categories.Length == 0) return false;
            if (_categories.Length > other!._categories.Length) return false;

            for (var i = 0; i < _categories.Length; i++) {
                if (_categories[i] == "*") continue;
                if (!_categories[i].Equals(other._categories[i]))
                    return false;
            }

            return true;
        } else if (obj is StreamId otherId) {
            return Equals((StreamKey)otherId);
        }

        return false;
    }
}