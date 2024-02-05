namespace BusinessDomain;
using System;

using LvStreamStore.Messaging;

public static partial class ItemMsgs {
    public record CreateItem(Guid ItemId, string Name) : Message;
    public record ChangeName(Guid ItemId, string Name) : Message;
    public record AddItems(int NumberOfItems) : Message;
}
