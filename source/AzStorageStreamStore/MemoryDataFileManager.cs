//using AzStorageStreamStore;

//public class MemoryDataFileManager : IDataFileManager {
//    private readonly MemoryStream _memoryStream = new();

//    /// <inheritdoc />
//    public int Checkpoint { get; private set; }

//    /// <inheritdoc />
//    public ValueTask<bool> ExistsAsync() => ValueTask.FromResult(true);

//    /// <inheritdoc />
//    public ValueTask<long> GetLengthAsync() => ValueTask.FromResult(_memoryStream.Length);

//    /// <inheritdoc />
//    public async ValueTask<int> ReadLogAsync(byte[] data, int length) => await _memoryStream.ReadAsync(data, 0, length);

//    /// <inheritdoc />
//    public async ValueTask<int> ReadLogAsync(byte[] data, int offset, int length) => await _memoryStream.ReadAsync(data, offset, length);

//    /// <inheritdoc />
//    public void Seek(long offset, SeekOrigin origin) => _memoryStream.Seek(offset, origin);

//    /// <inheritdoc />
//    public async Task WriteAsync(byte[] data) {
//        int endOfData = data.Length;

//        for (var i = 0; i < data.Length; i++) {
//            if (data[i] == 0x00) {
//                endOfData = i;
//                break;
//            }
//        }

//        var cPos = _memoryStream.Position;
//        _memoryStream.Seek(Checkpoint, SeekOrigin.Begin);
//        await _memoryStream.WriteAsync(data, 0, data.Length);
//        _memoryStream.Seek(cPos, SeekOrigin.Begin);

//        Checkpoint += endOfData;
//    }
//}
