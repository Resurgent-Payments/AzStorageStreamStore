namespace LvStreamStore.Authentication;

using System.Threading.Tasks;

using LvStreamStore.ApplicationToolkit;

internal class ApplicationTenantService : ReadModelBase,
    IAsyncCommandHandler<ApplicationTenantMsgs.Create>,
    IAsyncCommandHandler<ApplicationTenantMsgs.ChangeHostname>,
    IAsyncCommandHandler<ApplicationTenantMsgs.ChangeName>,
    IAsyncCommandHandler<ApplicationTenantMsgs.Lock>,
    IAsyncCommandHandler<ApplicationTenantMsgs.Unlock>,
    IAsyncCommandHandler<ApplicationTenantMsgs.Close>,
    IAsyncHandler<ApplicationTenantMsgs.Created>, 
    IAsyncHandler<ApplicationTenantMsgs.HostnameChanged> {
    public ApplicationTenantService(ISubscriber inBus, IStreamStoreRepository repository) : base(inBus, repository) {
        Subscribe<ApplicationTenantMsgs.Create>(this);
        Subscribe<ApplicationTenantMsgs.ChangeHostname>(this);
        Subscribe<ApplicationTenantMsgs.ChangeName>(this);
        Subscribe<ApplicationTenantMsgs.Lock>(this);
        Subscribe<ApplicationTenantMsgs.Unlock>(this);
        Subscribe<ApplicationTenantMsgs.Close>(this);

        SubscribeToStream<ApplicationTenant, ApplicationTenantMsgs.Created>(this);
        SubscribeToStream<ApplicationTenant, ApplicationTenantMsgs.HostnameChanged>(this);
    }

    public ValueTask<CommandResult> HandleAsync(ApplicationTenantMsgs.Create command) {
        throw new NotImplementedException();
    }

    public ValueTask<CommandResult> HandleAsync(ApplicationTenantMsgs.ChangeHostname command) {
        throw new NotImplementedException();
    }

    public ValueTask<CommandResult> HandleAsync(ApplicationTenantMsgs.ChangeName command) {
        throw new NotImplementedException();
    }

    public ValueTask<CommandResult> HandleAsync(ApplicationTenantMsgs.Lock command) {
        throw new NotImplementedException();
    }

    public ValueTask<CommandResult> HandleAsync(ApplicationTenantMsgs.Unlock command) {
        throw new NotImplementedException();
    }

    public ValueTask<CommandResult> HandleAsync(ApplicationTenantMsgs.Close command) {
        throw new NotImplementedException();
    }

    public ValueTask HandleAsync(ApplicationTenantMsgs.Created @event) {
        throw new NotImplementedException();
    }

    public ValueTask HandleAsync(ApplicationTenantMsgs.HostnameChanged @event) {
        throw new NotImplementedException();
    }
}
