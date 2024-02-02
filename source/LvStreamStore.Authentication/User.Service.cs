namespace LvStreamStore.Authentication;

using System.Threading.Tasks;

using LvStreamStore.ApplicationToolkit;

internal class UserService : ReadModelBase, IAsyncCommandHandler<UserMsgs.RegisterUser>, IAsyncCommandHandler<UserMsgs.ChangePassword>, IAsyncCommandHandler<UserMsgs.ChangePrimaryEmailAddress>, IAsyncHandler<UserMsgs.PasswordChanged> {
    private readonly IPasswordHasher _passwordHasher;
    private readonly Dictionary<Guid, string> _currentUserPasswords = new();

    public UserService(IDispatcher dispatcher, IStreamStoreRepository repository, IPasswordHasher passwordHasher) : base(dispatcher, repository) {
        _passwordHasher = passwordHasher;

        SubscribeToStream<User, UserMsgs.PasswordChanged>(this);
    }

    public async ValueTask<CommandResult> HandleAsync(UserMsgs.RegisterUser command) {
        var user = new User(command.UserId, command.TenantId, command.UserName, command.FirstName, command.LastName, command.PrimaryEmailAddress);
        var newPassword = GenerateRandomPassword();
        user.ChangePassword(newPassword);
        await Repository.Save(user);
        return new UserMsgs.UserHasBeenRegistered(command, newPassword, command.MsgId);
    }

    public async ValueTask<CommandResult> HandleAsync(UserMsgs.ChangePassword command) {
        var user = await Repository.TryGetById<User>(command.UserId);

        if (!_currentUserPasswords.TryGetValue(command.UserId, out var hashedPassword) && !_passwordHasher.VerifyPassword(command.CurrentPassword, hashedPassword ?? string.Empty)) {
            return command.Fail();
        }

        user.ChangePassword(_passwordHasher.HashPassword(command.NewPassword));

        await Repository.Save(user);

        return command.Complete();
    }

    public async ValueTask<CommandResult> HandleAsync(UserMsgs.ChangePrimaryEmailAddress command) {
        var user = await Repository.TryGetById<User>(command.UserId);
        user.ChangePrimaryEmailAddress(command.PrimaryEmailAddress);
        await Repository.Save(user);
        return command.Complete();
    }

    public ValueTask HandleAsync(UserMsgs.PasswordChanged @event) {
        _currentUserPasswords[@event.UserId] = @event.HashedPassword;
        return ValueTask.CompletedTask;
    }

    private static string GenerateRandomPassword() {
        int length = 10;
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_+";
        var random = new Random();
        string password = new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
        return password;
    }
}
