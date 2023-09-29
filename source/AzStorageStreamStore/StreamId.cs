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
}
