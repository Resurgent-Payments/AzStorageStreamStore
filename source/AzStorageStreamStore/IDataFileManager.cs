namespace AzStorageStreamStore;

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

/// <summary>
/// This only handles the raw read/write to the data file and WAL files.
/// </summary>
public interface IDataFileManager {
    /// <summary>
    /// This is the cursor position on where the last byte of data was written to.
    /// </summary>
    int Checkpoint { get; }

    /// <summary>
    /// Determines if the data file itself exists.
    /// </summary>
    /// <returns></returns>
    ValueTask<bool> ExistsAsync();

    /// <summary>
    /// Retrieves the amount of used storage for the managed data file
    /// </summary>
    /// <returns></returns>
    ValueTask<long> GetLengthAsync();

    /// <summary>
    /// Reads the log file from position 0, with the desired length.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    ValueTask<int> ReadLogAsync(byte[] data, int length);

    /// <summary>
    /// Allows the ability to read the log file.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="fromPosition">The 0-index starting position to start the read from.</param>
    /// <param name="length">Max. number of bytes to read.</param>
    /// <returns>Number of bytes read</returns>
    ValueTask<int> ReadLogAsync(byte[] data, int offset, int length);

    /// <summary>
    /// Sets the position of the "read" cursor to the specified value.
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="origin"></param>
    void Seek(long offset, SeekOrigin origin);

    /// <summary>
    /// This stores the provided data into the managed data store.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    /// <remarks>In most implementations, this method will write to a W.A.L., then internally, will move that data into the main data file.</remarks>
    Task WriteAsync(byte[] data);
}


public class MemoryDataFileManager : IDataFileManager {
    private readonly MemoryStream _memoryStream = new();

    /// <inheritdoc />
    public int Checkpoint { get; private set; }

    /// <inheritdoc />
    public ValueTask<bool> ExistsAsync() => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask<long> GetLengthAsync() => ValueTask.FromResult(_memoryStream.Length);

    /// <inheritdoc />
    public async ValueTask<int> ReadLogAsync(byte[] data, int length) => await _memoryStream.ReadAsync(data, 0, length);

    /// <inheritdoc />
    public async ValueTask<int> ReadLogAsync(byte[] data, int offset, int length) => await _memoryStream.ReadAsync(data, offset, length);

    /// <inheritdoc />
    public void Seek(long offset, SeekOrigin origin) => _memoryStream.Seek(offset, origin);

    /// <inheritdoc />
    public async Task WriteAsync(byte[] data) {
        int endOfData = data.Length;

        for (var i = 0; i < data.Length; i++) {
            if (data[i] == 0x00) {
                endOfData = i;
                break;
            }
        }

        var cPos = _memoryStream.Position;
        _memoryStream.Seek(Checkpoint, SeekOrigin.Begin);
        await _memoryStream.WriteAsync(data, 0, data.Length);
        _memoryStream.Seek(cPos, SeekOrigin.Begin);

        Checkpoint += endOfData;
    }
}


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

public class LocalDiskFileManagerOptions {
    public string BaseDataPath { get; set; }
    public int FileReadBlockSize { get; set; } = 4096;
}