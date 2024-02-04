namespace LvStreamStore.Messaging;

using System;

public class MissingHandlerException : Exception {
    public MissingHandlerException() { }

    public MissingHandlerException(string? message) : base(message) { }
}
