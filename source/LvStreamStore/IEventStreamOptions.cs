namespace LvStreamStore;
public abstract class EventStreamOptions {
    public abstract bool UseCaching { get; set; }
    public int CachePageSize { get; set; } = 500;
    public bool IsPollingEnabled { get; set; } = false;

}