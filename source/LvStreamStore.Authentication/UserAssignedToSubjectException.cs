namespace LvStreamStore.Authentication {
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    internal class UserAssignedToSubjectException : Exception {
        public UserAssignedToSubjectException() {
        }

        public UserAssignedToSubjectException(string? message) : base(message) {
        }

        public UserAssignedToSubjectException(string? message, Exception? innerException) : base(message, innerException) {
        }

        protected UserAssignedToSubjectException(SerializationInfo info, StreamingContext context) : base(info, context) {
        }
    }
}