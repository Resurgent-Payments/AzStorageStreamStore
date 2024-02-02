namespace LvStreamStore.Authentication;

using LvStreamStore.ApplicationToolkit;

using System;

public static partial class ApplicationTenantMsgs {
    public record Created(Guid ApplicationTenantId, string Name, string Hostname) : Event;
    public record HostnameChanged(Guid ApplicationTenantId, string Hostname) : Event;
    public record NameChanged(Guid ApplicationTenantId, string Name) : Event;
    public record Locked(Guid ApplicationTenantId) : Event;
    public record Unlocked(Guid ApplicationTenantId) : Event;
    public record Closed(Guid ApplicationTenantId) : Event;
}
