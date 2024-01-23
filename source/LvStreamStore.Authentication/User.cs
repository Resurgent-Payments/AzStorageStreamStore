namespace LvStreamStore.Authentication;

using LvStreamStore.ApplicationToolkit;

internal class User : AggregateRoot {
    private HashSet<(string SubjectId, string AuthenticationProvider, string AuthenticationDomain, string UserName)> _authenticationDomainMappings = new();
    private string _currentHashedPassword = string.Empty;
    private string _primaryEmailAddress = string.Empty;

    public User(Guid userId, Guid tenantId, string userName, string firstName, string lastName, string primaryEmailAddress) {
        RegisterEvents();
        Raise(new UserMsgs.Registered(userId, tenantId, userName, firstName, lastName));
        Raise(new UserMsgs.PrimaryEmailAddressChanged(userId, primaryEmailAddress));
    }

    public User() {
        RegisterEvents();
    }

    private void RegisterEvents() {
        Register<UserMsgs.Registered>(Apply);
        Register<UserMsgs.PasswordChanged>(Apply);
        Register<UserMsgs.PrimaryEmailAddressChanged>(Apply);
    }

    internal void ChangePassword(string hashedPassword) {
        if (_currentHashedPassword.Equals(hashedPassword)) { return; }
        Raise(new UserMsgs.PasswordChanged(Id, hashedPassword));
    }

    public void ChangePrimaryEmailAddress(string primaryEmailAddress) {
        if (_primaryEmailAddress.Equals(primaryEmailAddress)) { return; }
        Raise(new UserMsgs.PrimaryEmailAddressChanged(Id, primaryEmailAddress));
    }


    private void Apply(UserMsgs.Registered msg) {
        Id = msg.UserId;
    }

    private void Apply(UserMsgs.PasswordChanged msg) {
        _currentHashedPassword = msg.HashedPassword;
    }

    private void Apply(UserMsgs.PrimaryEmailAddressChanged msg) {
        _primaryEmailAddress = msg.EmailAddress;
    }
}
