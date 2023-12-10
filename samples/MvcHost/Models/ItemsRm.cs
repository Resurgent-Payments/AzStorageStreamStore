namespace MvcHost.Models {
    using System.Threading.Tasks;

    using BusinessDomain;

    using LvStreamStore.ApplicationToolkit;

    public class ItemsRm : ReadModelBase, IAsyncHandler<ItemMsgs.Created>, IAsyncHandler<ItemMsgs.NameChanged>, IAutoStartService {
        private List<ListItem> _items = new();

        public IReadOnlyList<ListItem> Items => _items.AsReadOnly();

        public ItemsRm(ISubscriber inBus, IStreamStoreRepository repository) : base(inBus, repository) {
            SubscribeToStream<Item, ItemMsgs.Created>(this);
            SubscribeToStream<Item, ItemMsgs.NameChanged>(this);
        }

        public ValueTask HandleAsync(ItemMsgs.Created @event) {
            _items.Add(new ListItem(@event.ItemId) { Name = @event.Name });
            return ValueTask.CompletedTask;
        }

        public ValueTask HandleAsync(ItemMsgs.NameChanged @event) {
            foreach (var item in _items.Where(i => i.ItemId == @event.ItemId).ToArray()) {
                item.Name = @event.Name;
            }

            return ValueTask.CompletedTask;
        }


        public record ListItem(Guid ItemId) {
            public string Name { get; set; }
        }
    }
}
