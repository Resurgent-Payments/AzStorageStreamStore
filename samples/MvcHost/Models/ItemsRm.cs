namespace MvcHost.Models {
    using System.Threading.Tasks;

    using BusinessDomain;

    using LvStreamStore.ApplicationToolkit;
    using LvStreamStore.Messaging;

    public class ItemsRm : ReadModelBase, IReceiver<ItemMsgs.Created>, IReceiver<ItemMsgs.NameChanged> {
        private List<ListItem> _items = new();

        public IReadOnlyList<ListItem> Items => _items.AsReadOnly();

        public ItemsRm(AsyncDispatcher inBus, IStreamStoreRepository repository) : base(inBus, repository) {
            SubscribeToStream<ItemMsgs.Created>(this);
            SubscribeToStream<ItemMsgs.NameChanged>(this);
        }

        public Task Receive(ItemMsgs.Created @event) {
            _items.Add(new ListItem(@event.ItemId) { Name = @event.Name });
            return Task.CompletedTask;
        }

        public Task Receive(ItemMsgs.NameChanged @event) {
            foreach (var item in _items.Where(i => i.ItemId == @event.ItemId).ToArray()) {
                item.Name = @event.Name;
            }
            return Task.CompletedTask;
        }

        public record ListItem(Guid ItemId) {
            public string Name { get; set; }
        }
    }
}
