namespace LvStreamStore.Messaging;

using System.Threading.Tasks;

public interface ISendAsync {
    public Task SendAsync(Message msg, TimeSpan? timesOutAfter = null);
}
