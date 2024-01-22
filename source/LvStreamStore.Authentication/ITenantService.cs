namespace LvStreamStore.Authentication {
    using System;

    public interface ITenantService : ITenantService<Guid> { }

    public interface ITenantService<TId> {
        TId GetTenantId();
    }

    internal class SingleTenantService : ITenantService {
        public Guid GetTenantId() => Guid.Empty;
    }
}
