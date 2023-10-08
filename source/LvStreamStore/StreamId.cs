namespace LvStreamStore;

using System.Diagnostics;

[DebuggerDisplay("{ToString()}")]
public class StreamId {
    public static StreamId NULL = new StreamId(string.Empty, Array.Empty<string>(), string.Empty);

    public readonly string TenantId;
    public readonly string[] Hierarchy;
    public readonly string ObjectId;

    public StreamId(string tenantId, string[] hierarchy, string objectId) {
        TenantId = tenantId;
        Hierarchy = hierarchy;
        ObjectId = objectId;
    }

    public static implicit operator StreamKey(StreamId id) {
        var list = new List<string> {
            id.TenantId
        };
        list.AddRange(id.Hierarchy);
        list.Add(id.ObjectId);
        return new(list.ToArray());
    }

    public static bool operator ==(StreamId streamId, StreamKey streamKey) => streamId?.Equals(streamKey) ?? (streamId is null && streamKey is null) ? true : false;
    public static bool operator !=(StreamId streamId, StreamKey streamKey) => !streamId?.Equals(streamKey) ?? (streamId is null && streamKey is null) ? false : true;
    public static bool operator ==(StreamId streamId1, StreamId streamId2) => streamId1?.Equals(streamId2) ?? false;
    public static bool operator !=(StreamId streamId1, StreamId streamId2) => !streamId1?.Equals(streamId2) ?? true;

    public override bool Equals(object? obj) {
        if (ReferenceEquals(this, obj)) return true;
        if (obj is not StreamId other) return false;

        if (!TenantId.Equals(other.TenantId)) return false;
        if (!ObjectId.Equals(other.ObjectId)) return false;

        if (Hierarchy?.Length > other!.Hierarchy.Length) return false;

        for (var i = 0; i < Hierarchy?.Length; i++) {
            if (Hierarchy[i] == "*") continue;
            if (!Hierarchy[i].Equals(other.Hierarchy[i]))
                return false;
        }

        return true;
    }

    public override int GetHashCode() {
        var hash = new HashCode();
        hash.Add(TenantId);
        foreach (var cat in Hierarchy) {
            hash.Add(cat.GetHashCode());
        }
        hash.Add(ObjectId);
        return hash.ToHashCode();
    }

    public override string ToString() {
        var list = Hierarchy.Any()
            ? Hierarchy.Aggregate((s1, s2) => $"{s1},{s2}")
            : string.Empty;

        return $"{TenantId}|{list}|{ObjectId}";
    }
}