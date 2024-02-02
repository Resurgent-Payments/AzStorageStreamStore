namespace LvStreamStore.Authentication {
    using System;

    using LvStreamStore.ApplicationToolkit;

    public static partial class UserMsgs {
        public record RegisterUser(Guid UserId, Guid TenantId, string UserName, string FirstName, string LastName, string PrimaryEmailAddress) : Command;
        public record ChangePassword(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmedNewPassword) : Command;
        public record ChangePrimaryEmailAddress(Guid UserId, string PrimaryEmailAddress) : Command;
    }
}
