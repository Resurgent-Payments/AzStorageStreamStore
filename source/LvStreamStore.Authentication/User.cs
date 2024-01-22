namespace LvStreamStore.Authentication;

using LvStreamStore.ApplicationToolkit;

internal class User : AggregateRoot {
    private HashSet<(string SubjectId, string AuthenticationProvider, string AuthenticationDomain, string UserName)> _authenticationDomainMappings = new();
    private string _currentHashedPassword = string.Empty;
    private string _primaryEmailAddress = string.Empty;

    public User(Guid userId, Guid tenantId, string userName, string firstName, string lastName, string primaryEmailAddress) {
        RegisterEvents();
        Raise(new UserMsgs.Created(userId, tenantId, userName, firstName, lastName));
        Raise(new UserMsgs.PrimaryEmailAddressChanged(userId, primaryEmailAddress));
    }

    public User() {
        RegisterEvents();
    }

    private void RegisterEvents() {
        Register<UserMsgs.Created>(Apply);
        Register<UserMsgs.MappedToAuthDomain>(Apply);
        Register<UserMsgs.PasswordChanged>(Apply);
        Register<UserMsgs.PrimaryEmailAddressChanged>(Apply);
    }

    public void MapToAuthDomain(string subjectId, string authProvider, string authDomain, string userName) {
        subjectId = subjectId.ToLowerInvariant();
        authProvider = authProvider.ToLowerInvariant();
        authDomain = authDomain.ToLowerInvariant();
        userName = userName.ToLowerInvariant();

        if (_authenticationDomainMappings.Contains((subjectId, authProvider, authDomain, userName))) { return; }
        Raise(new UserMsgs.MappedToAuthDomain(Id, subjectId, authProvider, authDomain, userName));
    }

    internal void ChangePassword(string hashedPassword) {
        if (_currentHashedPassword.Equals(hashedPassword)) { return; }
        Raise(new UserMsgs.PasswordChanged(Id, hashedPassword));
    }

    public void ChangePrimaryEmailAddress(string primaryEmailAddress) {
        if (_primaryEmailAddress.Equals(primaryEmailAddress)) { return; }
        Raise(new UserMsgs.PrimaryEmailAddressChanged(Id, primaryEmailAddress));
    }


    private void Apply(UserMsgs.Created msg) {
        Id = msg.UserId;
    }

    private void Apply(UserMsgs.MappedToAuthDomain msg) {
        _authenticationDomainMappings.Add((msg.SubjectId, msg.AuthProvider, msg.AuthDomain, msg.UserName));
    }

    private void Apply(UserMsgs.PasswordChanged msg) {
        _currentHashedPassword = msg.HashedPassword;
    }

    private void Apply(UserMsgs.PrimaryEmailAddressChanged msg) {
        _primaryEmailAddress = msg.EmailAddress;
    }
}
