namespace LvStreamStore.Messaging;

using System.Threading.Tasks;

internal abstract record AsyncHandler : Message {
    public abstract ValueTask HandleAsync(Message msg);
}
