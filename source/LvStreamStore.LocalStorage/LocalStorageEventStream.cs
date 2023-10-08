namespace LvStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using LvStreamStore.LocalStorage;

using Microsoft.Extensions.Options;

public class LocalStorageEventStream : EventStream {
    private readonly LocalStorageEventStreamOptions _options;
    private readonly string _dataFile;

    public LocalStorageEventStream(IOptions<EventStreamOptions> options) : base(options) {
        _options = options.Value as LocalStorageEventStreamOptions ?? new LocalStorageEventStreamOptions();
        _dataFile = Path.Combine(_options.BaseDataPath, "chunk.dat");

        if (!Directory.Exists(_options.BaseDataPath)) {
            Directory.CreateDirectory(_options.BaseDataPath);
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

    public override IAsyncEnumerator<StreamItem> GetAsyncEnumerator(CancellationToken token = default) => new LocalStorageEventStreamReader(_dataFile, _options, token);
}
