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

    internal LocalStorageEventStream(ILoggerFactory loggerFactory, IEventSerializer eventSerializer, IOptions<LocalStorageEventStreamOptions> options) : base(loggerFactory, options.Value!) {
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


    protected override async Task WriteAsync(StreamItem item) {
        var ser = _eventSerializer.Serialize(item);
        var bytes = BitConverter.GetBytes((int)ser.Length); // presume that this'll be 4-bytes.

        using (var fileWriter = new FileStream(_dataFile, new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Append, Options = FileOptions.Asynchronous, Share = FileShare.Read })) {
            fileWriter.Seek(0, SeekOrigin.End);

            // write header.

            await fileWriter.WriteAsync(bytes);
            await ser.CopyToAsync(fileWriter);
            await fileWriter.FlushAsync();
        }

        Checkpoint += ser.Length + LengthOfEventHeader;
    }

    bool _disposed = false;
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (_disposed || !disposing) return;

        _disposed = true;
    }

    public override EventStreamReader GetReader() => new LocalStorageEventStreamReader(_dataFile, _eventSerializer, _options);
}
