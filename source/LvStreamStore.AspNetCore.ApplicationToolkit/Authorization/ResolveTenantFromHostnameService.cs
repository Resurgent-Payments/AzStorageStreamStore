namespace LvStreamStore.ApplicationToolkit.Authorization;

using System;

using LvStreamStore.Authentication;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

internal class ResolveTenantFromHostnameService : ReadModelBase, ITenantService {
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ResolveTenantFromHostnameServiceOptions _options;

    public ResolveTenantFromHostnameService(ISubscriber inBus, IStreamStoreRepository repository, IHttpContextAccessor httpContextAccessor, IOptions<ResolveTenantFromHostnameServiceOptions> options) : base(inBus, repository) {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value ?? new();
    }

    public Guid GetTenantId() {
        return Guid.Empty;
    }
}
