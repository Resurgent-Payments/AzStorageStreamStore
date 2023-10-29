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
    private LocalStorageEventStreamOptions _localStorageOptions() => (LocalStorageEventStreamOptions)_options;

    internal LocalStorageEventStream(ILoggerFactory loggerFactory, IEventSerializer eventSerializer, IOptions<LocalStorageEventStreamOptions> options) : base(loggerFactory, eventSerializer, options.Value!) {
        _options = options.Value!;
        _dataFile = Path.Combine(_options.BaseDataPath, "chunk.dat");

        if (!Directory.Exists(_localStorageOptions().BaseDataPath)) {
            Directory.CreateDirectory(_localStorageOptions().BaseDataPath);
        }

        if (!File.Exists(_dataFile)) {
            File.Create(_dataFile).Dispose();
        }
    }

    protected override async Task WriteAsync(byte[] data) {
        var endOfData = data.Length;

        for (var i = 0; i < data.Length; i++) {
            if (data[i] == 0x00) {
                endOfData = i;
                break;
            }
        }

        using (var fileWriter = new FileStream(_dataFile, new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Append, Options = FileOptions.Asynchronous, Share = FileShare.Read })) {
            fileWriter.Seek(0, SeekOrigin.End);
            await fileWriter.WriteAsync(data, 0, data.Length);
            await fileWriter.FlushAsync();
        }

        Checkpoint += endOfData;
    }

    bool _disposed = false;
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (_disposed || !disposing) return;

        _disposed = true;
    }

    public override EventStreamReader GetReader() => new LocalStorageEventStreamReader(_dataFile, Serializer, _options);
}
