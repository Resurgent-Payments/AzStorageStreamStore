namespace LvStreamStore.Authentication {
    using System;

    using LvStreamStore.ApplicationToolkit;

    public static partial class UserMsgs {
        public record Registered(Guid UserId, Guid TenantId, string UserName, string FirstName, string LastName) : Event;
        public record PasswordChanged(Guid UserId, string HashedPassword) : Event;
        public record PrimaryEmailAddressChanged(Guid UserId, string EmailAddress) : Event;
    }

    public record UserHasBeenRegistered(Command SourceCommand, string Password, Guid? MsgId = null) :  CommandCompleted(SourceCommand, MsgId);
}
