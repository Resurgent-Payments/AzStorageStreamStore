namespace BusinessDomain;
using System;

using LvStreamStore.ApplicationToolkit;

public static partial class ItemMsgs {
    public record CreateItem(Guid ItemId, string Name) : Command;
    public record ChangeName(Guid ItemId, string Name) : Command;
    public record AddItems(int NumberOfItems) : Command;
}
