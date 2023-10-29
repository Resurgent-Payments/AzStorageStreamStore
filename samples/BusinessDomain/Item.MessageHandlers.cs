namespace BusinessDomain;

using System.Threading.Tasks;

using LvStreamStore.ApplicationToolkit;

using Microsoft.Extensions.Logging;

public class ItemMsgHandlers : TransientSubscriber, IAsyncCommandHandler<ItemMsgs.CreateItem>, IAsyncCommandHandler<ItemMsgs.ChangeName> {
    private readonly IStreamStoreRepository _repository;

    public ItemMsgHandlers(IStreamStoreRepository repository, IDispatcher dispatcher, ILoggerFactory factory) : base(dispatcher, factory) {
        _repository = repository;

        Subscribe<ItemMsgs.CreateItem>(this);
        Subscribe<ItemMsgs.ChangeName>(this);
    }

    public async ValueTask<CommandResult> HandleAsync(ItemMsgs.CreateItem command) {
        var item = new Item(command.ItemId, command.Name);
        return await _repository.Save(item)
            ? command.Complete()
            : command.Fail();
    }

    public async ValueTask<CommandResult> HandleAsync(ItemMsgs.ChangeName command) {
        var item = await _repository.TryGetById<Item>(command.ItemId);
        item.Rename(command.Name);
        return await _repository.Save(item)
            ? command.Complete()
            : command.Fail();
    }
}
