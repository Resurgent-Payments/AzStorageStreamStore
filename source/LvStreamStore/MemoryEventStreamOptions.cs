namespace LvStreamStore;

public class MemoryEventStreamOptions : EventStreamOptions {
    public int PageSize { get; set; } = 100;
}
