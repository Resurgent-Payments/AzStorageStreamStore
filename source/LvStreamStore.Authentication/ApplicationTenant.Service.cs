namespace LvStreamStore.Authentication;

using System.Threading.Tasks;

using LvStreamStore.ApplicationToolkit;
using LvStreamStore.Messaging;

internal class ApplicationTenantService : ReadModelBase,
    IHandleAsync<ApplicationTenantMsgs.Create>,
    IHandleAsync<ApplicationTenantMsgs.ChangeHostname>,
    IHandleAsync<ApplicationTenantMsgs.ChangeName>,
    IHandleAsync<ApplicationTenantMsgs.Lock>,
    IHandleAsync<ApplicationTenantMsgs.Unlock>,
    IHandleAsync<ApplicationTenantMsgs.Close>,
    IReceiver<ApplicationTenantMsgs.Created>, 
    IReceiver<ApplicationTenantMsgs.HostnameChanged> {
    public ApplicationTenantService(AsyncDispatcher dispatcher, IStreamStoreRepository repository) : base(dispatcher, repository) {
        Subscribe<ApplicationTenantMsgs.Create>(this);
        Subscribe<ApplicationTenantMsgs.ChangeHostname>(this);
        Subscribe<ApplicationTenantMsgs.ChangeName>(this);
        Subscribe<ApplicationTenantMsgs.Lock>(this);
        Subscribe<ApplicationTenantMsgs.Unlock>(this);
        Subscribe<ApplicationTenantMsgs.Close>(this);

        SubscribeToStream< ApplicationTenantMsgs.Created>(this);
        SubscribeToStream< ApplicationTenantMsgs.HostnameChanged>(this);
    }

    public Task HandleAsync(ApplicationTenantMsgs.Create command) {
        throw new NotImplementedException();
    }

    public Task HandleAsync(ApplicationTenantMsgs.ChangeHostname command) {
        throw new NotImplementedException();
    }

    public Task HandleAsync(ApplicationTenantMsgs.ChangeName command) {
        throw new NotImplementedException();
    }

    public Task HandleAsync(ApplicationTenantMsgs.Lock command) {
        throw new NotImplementedException();
    }

    public Task HandleAsync(ApplicationTenantMsgs.Unlock command) {
        throw new NotImplementedException();
    }

    public Task HandleAsync(ApplicationTenantMsgs.Close command) {
        throw new NotImplementedException();
    }

    public Task Receive(ApplicationTenantMsgs.Created @event) {
        throw new NotImplementedException();
    }

    public Task Receive(ApplicationTenantMsgs.HostnameChanged @event) {
        throw new NotImplementedException();
    }
}
