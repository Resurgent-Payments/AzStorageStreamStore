namespace BusinessDomain;
using LvStreamStore.ApplicationToolkit;

public class Item : AggregateRoot {
    private string _name = string.Empty;

    public Item(Guid itemId, string name) {
        Ensure.NotEmptyGuid(itemId, nameof(itemId));
        Ensure.NotNullOrEmpty(name, nameof(name));

        RegisterEvents();

        Raise(new ItemMsgs.Created(itemId, name));
    }

    public Item() {
        RegisterEvents();
    }

    private void RegisterEvents() {
        Register<ItemMsgs.Created>(Apply);
        Register<ItemMsgs.NameChanged>(Apply);
    }

    public void Rename(string name) {
        if (_name.Equals(name)) return;

        Raise(new ItemMsgs.NameChanged(Id, name, DateOnly.FromDateTime(DateTime.Now), TimeOnly.FromDateTime(DateTime.Now)));
    }


    private void Apply(ItemMsgs.Created msg) {
        Id = msg.ItemId;
    }

    private void Apply(ItemMsgs.NameChanged msg) {
        _name = msg.Name;
    }
}
