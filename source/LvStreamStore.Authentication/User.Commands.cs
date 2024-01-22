namespace LvStreamStore.Authentication {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using LvStreamStore.ApplicationToolkit;

    public static partial class UserMsgs {
        public record ChangePassword(Guid UserId, string CurrentPassword, string NewPassword, string ConfirmedNewPassword) : Command;
    }
}
