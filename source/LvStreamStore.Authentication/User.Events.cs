namespace LvStreamStore.Authentication {
    using System;

    using LvStreamStore.ApplicationToolkit;

    public static partial class UserMsgs {
        public record Created(Guid UserId, Guid TenantId, string UserName, string FirstName, string LastName) : Event;
        public record MappedToAuthDomain(Guid UserId, string SubjectId, string AuthProvider, string AuthDomain, string UserName) : Event;
        public record PasswordChanged(Guid UserId, string HashedPassword) : Event;
        public record PrimaryEmailAddressChanged(Guid UserId, string EmailAddress) : Event;
    }
}
