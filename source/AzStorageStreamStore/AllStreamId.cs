namespace AzStorageStreamStore;

public record AllStream(string TenantId, string Id) : StreamId(TenantId, Id) {
    public static AllStream SingleTenant = new AllStream("", "");
    public static AllStream MultiTenant = new AllStream("$sys", "");
}