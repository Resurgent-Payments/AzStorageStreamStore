namespace LvStreamStore.Authentication;

using System.Threading.Tasks;

using LvStreamStore.ApplicationToolkit;

using Microsoft.Extensions.Logging;

internal class UserService : TransientSubscriber, IAsyncCommandHandler<UserMsgs.ChangePassword> {
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IDispatcher dispatcher, ILoggerFactory factory, IPasswordHasher passwordHasher) : base(dispatcher, factory) {
        _passwordHasher = passwordHasher;
    }

    public ValueTask<CommandResult> HandleAsync(UserMsgs.ChangePassword command) {
        throw new NotImplementedException();
    }
}
