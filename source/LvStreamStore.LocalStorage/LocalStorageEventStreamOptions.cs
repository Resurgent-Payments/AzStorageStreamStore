namespace LvStreamStore;

public class LocalStorageEventStreamOptions : EventStreamOptions {
    public string BaseDataPath { get; set; } = string.Empty;
    public int FileReadBlockSize { get; set; } = 4096; // = 4_194_304; // 4mb per read
    public int ChunkFileSizeMB { get; set; } = 250;
    public override bool UseCaching { get; set; } = false;
}
