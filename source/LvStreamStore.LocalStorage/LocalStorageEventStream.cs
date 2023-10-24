namespace LvStreamStore;

using System.IO;
using System.Threading.Tasks;

using LvStreamStore.LocalStorage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class LocalStorageEventStream : EventStream {
    private readonly string _dataFile;
    private LocalStorageEventStreamOptions _localStorageOptions() => (LocalStorageEventStreamOptions)_options;

    public LocalStorageEventStream(ILoggerFactory loggerFactory, IOptions<EventStreamOptions> options) : base(loggerFactory, options) {
        _dataFile = Path.Combine(_localStorageOptions().BaseDataPath, "chunk.dat");

        if (!Directory.Exists(_localStorageOptions().BaseDataPath)) {
            Directory.CreateDirectory(_localStorageOptions().BaseDataPath);
        }

        if (!File.Exists(_dataFile)) {
            File.Create(_dataFile).Dispose();
        }

        AfterConstructed();
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

    public override EventStreamReader GetReader() => new LocalStorageEventStreamReader(_dataFile, _localStorageOptions());
}
