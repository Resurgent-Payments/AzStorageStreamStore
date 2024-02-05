namespace LvStreamStore.Authentication;

using LvStreamStore.Messaging;

public static partial class ApplicationTenantMsgs {
    public record Create(Guid ApplicationTenantId, string Name, string Hostname) : Message;
    public record ChangeHostname(Guid ApplicationTenantId, string Hostname) : Message;
    public record ChangeName(Guid ApplicationTenantId, string Name) : Message;
    public record Lock(Guid ApplicationTenantId) : Message;
    public record Unlock(Guid ApplicationTenantId) : Message;
    public record Close(Guid ApplicationTenantId) : Message;
}
