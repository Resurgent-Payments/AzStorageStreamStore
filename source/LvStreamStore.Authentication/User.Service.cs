namespace LvStreamStore.Authentication;

using System.Threading.Tasks;

using LvStreamStore.ApplicationToolkit;
using LvStreamStore.Messaging;

internal class UserService : ReadModelBase, IHandleAsync<UserMsgs.RegisterUser>, IHandleAsync<UserMsgs.ChangePassword>, IHandleAsync<UserMsgs.ChangePrimaryEmailAddress>, IReceiver<UserMsgs.PasswordChanged> {
    private readonly IPasswordHasher _passwordHasher;
    private readonly Dictionary<Guid, string> _currentUserPasswords = new();

    public UserService(AsyncDispatcher dispatcher, IStreamStoreRepository repository, IPasswordHasher passwordHasher) : base(dispatcher, repository) {
        _passwordHasher = passwordHasher;

        SubscribeToStream<UserMsgs.PasswordChanged>(this);
    }

    public async Task HandleAsync(UserMsgs.RegisterUser command) {
        try {
            var user = new User(command.UserId, command.TenantId, command.UserName, command.FirstName, command.LastName, command.PrimaryEmailAddress);
            var newPassword = GenerateRandomPassword();
            user.ChangePassword(newPassword);
            await Repository.Save(user);
            command.OnRegistrationComplete.SetResult(new UserMsgs.UserHasBeenRegistered(newPassword));
        }
        catch (Exception exc) {
            command.OnRegistrationComplete.SetException(exc);
        }
    }

    public async Task HandleAsync(UserMsgs.ChangePassword command) {
        var user = await Repository.TryGetById<User>(command.UserId);

        if (!_currentUserPasswords.TryGetValue(command.UserId, out var hashedPassword) && !_passwordHasher.VerifyPassword(command.CurrentPassword, hashedPassword ?? string.Empty)) {
            throw new Exception();
        }

        user.ChangePassword(_passwordHasher.HashPassword(command.NewPassword));

        await Repository.Save(user);
    }

    public async Task HandleAsync(UserMsgs.ChangePrimaryEmailAddress command) {
        var user = await Repository.TryGetById<User>(command.UserId);
        user.ChangePrimaryEmailAddress(command.PrimaryEmailAddress);
        await Repository.Save(user);
    }

    public Task Receive(UserMsgs.PasswordChanged @event) {
        _currentUserPasswords[@event.UserId] = @event.HashedPassword;
        return Task.CompletedTask;
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
