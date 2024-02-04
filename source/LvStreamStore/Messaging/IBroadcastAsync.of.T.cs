namespace LvStreamStore.Messaging;

using System.Threading.Tasks;

public interface IBroadcastAsync<TMessage> where TMessage : Message {
    Task BroadcastAsync(TMessage message);
}
