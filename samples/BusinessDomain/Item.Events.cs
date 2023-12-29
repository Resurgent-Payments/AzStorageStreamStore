namespace BusinessDomain;
using System;

using LvStreamStore.ApplicationToolkit;

public static partial class ItemMsgs {
    public record Created(Guid ItemId, string Name) : Event;
    public record NameChanged(Guid ItemId, string Name) : Event;
}
