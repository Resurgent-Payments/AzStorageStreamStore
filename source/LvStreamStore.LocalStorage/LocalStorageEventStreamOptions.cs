namespace LvStreamStore;

public class LocalStorageEventStreamOptions : EventStreamOptions {
    public string BaseDataPath { get; set; } = string.Empty;
    public int FileReadBlockSize { get; set; } = 4096;
}
