namespace LvStreamStore.LocalStorage {
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal class LocalStorageEventStreamReader : EventStreamReader {
        private readonly LocalStorageEventStreamOptions _options;
        private readonly string _dataFile;
        private readonly CancellationToken _token;
        private byte[] _buffer = new byte[4096];
        private int _position = 0;

        public LocalStorageEventStreamReader(string dataFileName, LocalStorageEventStreamOptions options, CancellationToken token = default) {
            _dataFile = dataFileName;
            _options = options;
        }

        public StreamItem Current { get; private set; }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask<bool> MoveNextAsync() {
            var ms = new MemoryStream();

            using (var fStream = new FileStream(_dataFile, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Options = FileOptions.Asynchronous, Share = FileShare.ReadWrite })) {
                fStream.Seek(_position, SeekOrigin.Begin);
                int offset;
                do {
                    Array.Clear(_buffer);
                    offset = await fStream.ReadAsync(_buffer, 0, _buffer.Length, _token);

                    for (var idx = 0; idx < offset; idx++) {
                        if (_buffer[idx] == StreamConstants.NULL) break; // if null, then no further data exists.

                        if (_buffer[idx] == StreamConstants.EndOfRecord) { // found a point whereas we need to deserialize what we have in the buffer, yield it back to the caller, then advance the index by 1.
                            ms.Seek(0, SeekOrigin.Begin);

                            Current = JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions)!;

                            idx += 1;
                            _position += idx;
                            ms?.Dispose();

                            return true;
                        }

                        ms.WriteByte(_buffer[idx]);
                    }
                    _position += offset;
                } while (offset != 0);
            }

            return false;
        }
    }
}
