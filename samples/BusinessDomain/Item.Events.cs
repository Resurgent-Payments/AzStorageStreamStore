namespace BusinessDomain;
using System;

using LvStreamStore.ApplicationToolkit;

public static partial class ItemMsgs {
    public record Created(Guid ItemId, string Name, Guid? MsgId = null) : Event(MsgId);
    public record NameChanged(Guid ItemId, string Name, Guid? MsgId = null) : Event(MsgId);
}
