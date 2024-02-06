namespace LvStreamStore;

using System.IO;
using System.Threading.Tasks;

using LvStreamStore.LocalStorage;
using LvStreamStore.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class LocalStorageEventStream : EventStream {
    private readonly LocalStorageEventStreamOptions _options;
    private readonly string _dataFile;
    private readonly IEventSerializer _eventSerializer;

    private LocalStorageEventStreamOptions _localStorageOptions() => (LocalStorageEventStreamOptions)_options;

    public LocalStorageEventStream(ILoggerFactory loggerFactory, IEventSerializer eventSerializer, IOptions<LocalStorageEventStreamOptions> options) : base(loggerFactory, options.Value!) {
        _options = options.Value!;
        _dataFile = Path.Combine(_options.BaseDataPath, "chunk.dat");
        _eventSerializer = eventSerializer;

        if (!Directory.Exists(_localStorageOptions().BaseDataPath)) {
            Directory.CreateDirectory(_localStorageOptions().BaseDataPath);
        }

        if (!File.Exists(_dataFile)) {
            File.Create(_dataFile).Dispose();
        }
    }


    protected override async Task WriteAsync(params StreamMessage[] items) {
        var metadataFile = Path.Combine(_options.BaseDataPath, "metadata.dat");
        FileMetadata? metadata;

        if (File.Exists(metadataFile)) {
            using (var reader = File.OpenRead(metadataFile)) {
                metadata = _eventSerializer.Deserialize<FileMetadata>(reader);
                await reader.DisposeAsync();
            }
        } else {
            metadata = new();
        }

        using (var writer = new FileStream(_dataFile, new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.OpenOrCreate, Options = FileOptions.Asynchronous, Share = FileShare.Read })) {
            writer.Seek(metadata.CheckPoint, SeekOrigin.Begin);

            foreach (var item in items) {
                var ser = _eventSerializer.Serialize(item);
                var bytes = BitConverter.GetBytes((int)ser.Length); // presume that this'll be 4-bytes.
                await writer.WriteAsync(bytes);
                await ser.CopyToAsync(writer);
                metadata.CheckPoint += ser.Length + LengthOfEventHeader;

                if (_options.UseCaching) {
                    Cache.Append(item);
                }
            }

            await writer.FlushAsync();
        }

        if (File.Exists(metadataFile)) {
            File.Delete(metadataFile);
        }

        using (var writer = new FileStream(metadataFile, new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Create, Options = FileOptions.WriteThrough, Share = FileShare.None })) {
            await _eventSerializer.Serialize(metadata).CopyToAsync(writer);
            await writer.FlushAsync();
            await writer.DisposeAsync();
        }
    }

    bool _disposed = false;
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (_disposed || !disposing) return;

        _disposed = true;
    }

    public override EventStreamReader GetReader() => new LocalStorageEventStreamReader(_dataFile, _eventSerializer, _options);
}