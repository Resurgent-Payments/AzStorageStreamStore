namespace AzStorageStreamStore;

using Microsoft.Extensions.Options;

public sealed class LocalDiskFileManager : IDataFileManager, IDisposable {
    LocalDiskFileManagerOptions _options;
    private readonly string _dataFile;
    private readonly FileStream _stream;

    public LocalDiskFileManager(IOptions<LocalDiskFileManagerOptions> options) {
        _options = options.Value ?? new();
        _dataFile = Path.Combine(_options.BaseDataPath, "chunk.dat");

        if (!Directory.Exists(_options.BaseDataPath)) {
            Directory.CreateDirectory(_options.BaseDataPath);
        }

        if (!File.Exists(_dataFile))
            File.Create(_dataFile).Dispose();

        _stream = new FileStream(_dataFile, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Options = FileOptions.Asynchronous, Share = FileShare.ReadWrite });
    }

    public int Checkpoint {
        get;
        private set;
    }

    public ValueTask<bool> ExistsAsync() => ValueTask.FromResult(File.Exists(_dataFile));

    public ValueTask<long> GetLengthAsync() => ValueTask.FromResult(new FileInfo(_dataFile).Length);

    public async ValueTask<int> ReadLogAsync(byte[] data, int length) => await _stream.ReadAsync(data, 0, length);

    public async ValueTask<int> ReadLogAsync(byte[] data, int offset, int length) => await _stream.ReadAsync(data, offset, length);

    public void Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

    public async Task WriteAsync(byte[] data) {
        int endOfData = data.Length;

        for (var i = 0; i < data.Length; i++) {
            if (data[i] == 0x00) {
                endOfData = i;
                break;
            }
        }

        using (var writeStream = new FileStream(_dataFile, new FileStreamOptions { Access = FileAccess.Write, Mode = FileMode.Append, Options = FileOptions.Asynchronous, Share = FileShare.Read })) {
            writeStream.Seek(0, SeekOrigin.End);
            await writeStream.WriteAsync(data, 0, data.Length);
            await writeStream.FlushAsync();
        }

        Checkpoint += endOfData;
    }

    public void Dispose() {
        _stream?.Dispose();
    }
}
