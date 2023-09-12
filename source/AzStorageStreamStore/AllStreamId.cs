namespace AzStorageStreamStore;

public record AllStreamId(string TenantId, string Id) : StreamId(TenantId, Id) {
    public static AllStreamId SingleTenant = new AllStreamId("", "");
    public static AllStreamId MultiTenant = new AllStreamId("$sys", "");
}