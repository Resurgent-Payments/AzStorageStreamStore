namespace LvStreamStore.Authentication {
    using System;

    using LvStreamStore.Messaging;

    public static partial class UserMsgs {
        public record RegisterUser(Guid UserId, Guid TenantId, string UserName, string FirstName, string LastName, string PrimaryEmailAddress, TaskCompletionSource<UserHasBeenRegistered> OnRegistrationComplete) : Message;
        public record ChangePassword(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmedNewPassword) : Message;
        public record ChangePrimaryEmailAddress(Guid UserId, string PrimaryEmailAddress) : Message;
    }
}
