namespace BusinessDomain;
using System;

using LvStreamStore.ApplicationToolkit;

public static partial class ItemMsgs {
    public record CreateItem(Guid ItemId, string Name, Guid? MsgId = null) : Command(MsgId: MsgId);
    public record ChangeName(Guid ItemId, string Name, Guid? MsgId = null) : Command(MsgId: MsgId);
    public record AddItems(int NumberOfItems, Guid? MsgId = null) : Command(MsgId: MsgId);
}
