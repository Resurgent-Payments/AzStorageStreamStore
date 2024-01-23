namespace LvStreamStore.Authentication;

using System;

using LvStreamStore.ApplicationToolkit;

internal class ApplicationTenant : AggregateRoot {
    private string _name = string.Empty;
    private string _hostname = string.Empty;
    private bool _locked = false;
    private bool _closed = false;

    public ApplicationTenant(Guid applicationTenantId, string Name, string hostName) {
        RegisterEvents();

        Raise(new ApplicationTenantMsgs.Created(applicationTenantId, Name, hostName));
    }

    public ApplicationTenant() {
        RegisterEvents();
    }

    private void RegisterEvents() {
        Register<ApplicationTenantMsgs.Created>(Apply);
        Register<ApplicationTenantMsgs.NameChanged>(Apply);
        Register<ApplicationTenantMsgs.HostnameChanged>(Apply);
        Register<ApplicationTenantMsgs.Locked>(Apply);
        Register<ApplicationTenantMsgs.Unlocked>(Apply);
        Register<ApplicationTenantMsgs.Closed>(Apply);
    }

    public void Rename(string name) {
        if (_closed) { throw new InvalidOperationException(); }
        if (_name.Equals(name)) { return; }
        Raise(new ApplicationTenantMsgs.NameChanged(Id, name));
    }

    public void ChangeHostname(string hostname) {
        if (_closed) { throw new InvalidOperationException(); }
        if (_hostname.Equals(hostname)) { return; }
        Raise(new ApplicationTenantMsgs.HostnameChanged(Id, hostname));
    }

    public void Lock() {
        if (_closed) { throw new InvalidOperationException(); }
        if (_locked) { return; }
        Raise(new ApplicationTenantMsgs.Locked(Id));
    }
    public void Unlock() {
        if (_closed) { throw new InvalidOperationException(); }
        if (!_locked) { return; }
        Raise(new ApplicationTenantMsgs.Unlocked(Id));
    }
    public void Close() {
        if (_closed) { return; }
        Raise(new ApplicationTenantMsgs.Closed(Id));
    }

    private void Apply(ApplicationTenantMsgs.Created msg) {
        Id = msg.ApplicationTenantId;
        _name = msg.Name;
        _hostname = msg.Hostname;
        _closed = false;
        _locked = false;
    }

    private void Apply(ApplicationTenantMsgs.NameChanged msg)
        => _name = msg.Name;

    private void Apply(ApplicationTenantMsgs.HostnameChanged msg)
        => _hostname = msg.Hostname;

    private void Apply(ApplicationTenantMsgs.Locked _)
        => _locked = true;

    private void Apply(ApplicationTenantMsgs.Unlocked _)
        => _locked = false;

    private void Apply(ApplicationTenantMsgs.Closed _)
        => _closed = true;
}
