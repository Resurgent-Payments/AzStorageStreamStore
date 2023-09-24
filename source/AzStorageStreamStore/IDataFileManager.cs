namespace AzStorageStreamStore {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// This only handles the raw read/write to the data file and WAL files.
    /// </summary>
    public interface IDataFileManager {
        /// <summary>
        /// This is the cursor position on where the last byte of data was written to.
        /// </summary>
        int Checkpoint { get; }

        /// <summary>
        /// The cursor position for subsequent reads.
        /// </summary>
        int Position { get; }

        /// <summary>
        /// Determines if the data file itself exists.
        /// </summary>
        /// <returns></returns>
        ValueTask<bool> ExistsAsync();

        /// <summary>
        /// Retrieves the amount of used storage for the managed data file
        /// </summary>
        /// <returns></returns>
        ValueTask<int> GetLengthAsync();

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
        MemoryStream _memoryStream = new();

        /// <inheritdoc />
        public int Checkpoint { get; private set; }

        /// <inheritdoc />
        public int Position { get; private set; }

        /// <inheritdoc />
        public ValueTask<bool> ExistsAsync() => ValueTask.FromResult(true);

        /// <inheritdoc />
        public ValueTask<int> GetLengthAsync() => ValueTask.FromResult(Convert.ToInt32(_memoryStream.Length));

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

            _memoryStream.Seek(Checkpoint, SeekOrigin.Begin);
            await _memoryStream.WriteAsync(data, 0, data.Length);

            Checkpoint += endOfData;
        }
    }
}
