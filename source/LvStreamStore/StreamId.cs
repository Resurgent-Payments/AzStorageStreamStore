using AzStorageStreamStore;

/// <summary>
/// the "id" of an actual stream.
/// </summary>
/// <param name="TenantId"></param>
/// <param name="Id"></param>
public record StreamId(string TenantId, string[] Hierarchy, string Id) {
    public static implicit operator StreamKey(StreamId id) {
        var list = new List<string> {
            id.TenantId
        };
        list.AddRange(id.Hierarchy);
        list.Add(id.Id);
        return new(list.ToArray());
    }

    public static bool operator ==(StreamId id, StreamKey key) => key.Equals(id);
    public static bool operator !=(StreamId id, StreamKey key) => !key.Equals(id);

    public override int GetHashCode() {
        var hash = new HashCode();
        hash.Add(TenantId);
        foreach (var cat in Hierarchy) {
            hash.Add(cat.GetHashCode());
        }
        hash.Add(Id);
        return hash.ToHashCode();
    }

    public virtual bool Equals(StreamId? other) {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other)) return true;

        if (!TenantId.Equals(other.TenantId)) return false;
        if (!Id.Equals(other.Id)) return false;

        if (Hierarchy.Length > other.Hierarchy.Length) return false;

        for (var i = 0; i < Hierarchy.Length; i++) {
            if (!Hierarchy[i].Equals(other.Hierarchy[i]))
                return false;
        }

        return true;
    }
}
