using LvStreamStore.ApplicationToolkit;

namespace LvStreamStore.Authentication;

using System.Threading.Tasks;

internal class ApplicationTenantService : ReadModelBase, IAsyncHandler<ApplicationTenantMsgs.Approved> {
    public ApplicationTenantService(ISubscriber inBus, IStreamStoreRepository repository) : base(inBus, repository) {
        SubscribeToStream<ApplicationTenant, ApplicationTenantMsgs.Approved>(this);
        SubscribeToStream<ApplicationTenant, ApplicationTenantMsgs.HostnameChanged>(this);
    }

    public ValueTask HandleAsync(ApplicationTenantMsgs.Approved @event) {
        throw new NotImplementedException();
    }
}
