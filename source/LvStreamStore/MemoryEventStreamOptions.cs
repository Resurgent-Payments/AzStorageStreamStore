namespace LvStreamStore;

public class MemoryEventStreamOptions : EventStreamOptions {
    public int PageSize { get; set; } = 100;
    public override bool UseCaching { get => false; set { } }
}
