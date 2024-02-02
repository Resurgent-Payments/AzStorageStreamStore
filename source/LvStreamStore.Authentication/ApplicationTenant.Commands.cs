namespace LvStreamStore.Authentication;

using LvStreamStore.ApplicationToolkit;

public static partial class ApplicationTenantMsgs {
    public record Create(Guid ApplicationTenantId, string Name, string Hostname) : Command;
    public record ChangeHostname(Guid ApplicationTenantId, string Hostname) : Command;
    public record ChangeName(Guid ApplicationTenantId, string Name) : Command;
    public record Lock(Guid ApplicationTenantId) : Command;
    public record Unlock(Guid ApplicationTenantId) : Command;
    public record Close(Guid ApplicationTenantId) : Command;
}
