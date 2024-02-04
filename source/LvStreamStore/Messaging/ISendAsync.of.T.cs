namespace LvStreamStore.Messaging;

using System.Threading.Tasks;

public interface ISendAsync<T> where T : Message{
    public Task SendAsync(T msg);
}
