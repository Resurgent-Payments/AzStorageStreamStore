namespace BusinessDomain;

using System.Threading.Tasks;

using LvStreamStore.ApplicationToolkit;
using LvStreamStore.Messaging;

public class ItemMsgHandlers : TransientSubscriber, IHandleAsync<ItemMsgs.CreateItem>, IHandleAsync<ItemMsgs.ChangeName>, IHandleAsync<ItemMsgs.AddItems> {
    private readonly IStreamStoreRepository _repository;

    public ItemMsgHandlers(IStreamStoreRepository repository, AsyncDispatcher dispatcher) : base(dispatcher) {
        _repository = repository;

        Subscribe<ItemMsgs.CreateItem>(this);
        Subscribe<ItemMsgs.ChangeName>(this);
        Subscribe<ItemMsgs.AddItems>(this);
    }

    public async Task HandleAsync(ItemMsgs.CreateItem command) {
        var item = new Item(command.ItemId, command.Name);
        if (!await _repository.Save(item)) { throw new Exception(); }
    }

    public async Task HandleAsync(ItemMsgs.ChangeName command) {
        var item = await _repository.TryGetById<Item>(command.ItemId);
        item.Rename(command.Name);
        if (!await _repository.Save(item)) { throw new Exception(); }
    }

    public async Task HandleAsync(ItemMsgs.AddItems command) {
        for (var x = 1; x <= command.NumberOfItems; x++) {
            if (!await _repository.Save(new Item(Guid.NewGuid(), $"Item #{x}"))) { throw new Exception(); }
        }
    }
}
