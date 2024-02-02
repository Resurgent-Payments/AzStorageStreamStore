namespace LvStreamStore.Authentication {
    using System;

    [Serializable]
    internal class UserAssignedToSubjectException : Exception {
        public UserAssignedToSubjectException() {
        }

        public UserAssignedToSubjectException(string? message) : base(message) {
        }

        public UserAssignedToSubjectException(string? message, Exception? innerException) : base(message, innerException) {
        }
    }
}