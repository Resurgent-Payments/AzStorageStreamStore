namespace LvStreamStore.LocalStorage {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using LvStreamStore.Serialization;

    internal class LocalStorageEventStreamReader : EventStreamReader {
        private readonly string _dataFile;
        private readonly IEventSerializer _eventSerializer;
        private readonly LocalStorageEventStreamOptions _options;
        private int _lastBytePosition = 0;

        public LocalStorageEventStreamReader(string dataFileName, IEventSerializer eventSerializer, LocalStorageEventStreamOptions options) {
            _dataFile = dataFileName;
            _eventSerializer = eventSerializer;
            _options = options;
        }

        public override IAsyncEnumerator<StreamItem> GetAsyncEnumerator(CancellationToken token = default)
            => new Enumerator(this, _eventSerializer, token);

        class Enumerator : IStreamEnumerator {
            private readonly IEventSerializer _eventSerializer;
            private readonly LocalStorageEventStreamReader _reader;
            private int _lastBytePosition;

            public StreamItem Current { get; private set; }



            public Enumerator(LocalStorageEventStreamReader reader, IEventSerializer eventSerializer, CancellationToken token = default) {
                _eventSerializer = eventSerializer;
                _reader = reader;

                _lastBytePosition = _reader._lastBytePosition;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public async ValueTask<bool> MoveNextAsync() {
                int readOffset;
                byte[] headerBuffer = new byte[EventStream.LengthOfEventHeader];
                byte[] readBuffer = new byte[4096];

                using (var fStream = new FileStream(_reader._dataFile, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Options = FileOptions.Asynchronous, Share = FileShare.ReadWrite })) {
                    if (fStream.Length <= _lastBytePosition) {
                        Current = null;
                        return false;
                    }


                    fStream.Seek(_lastBytePosition, SeekOrigin.Begin);

                    // read event header bytes
                    Array.Clear(headerBuffer);
                    readOffset = await fStream.ReadAsync(headerBuffer, 0, headerBuffer.Length);

                    if (readOffset <= 0) {
                        Current = null;
                        return false;
                    }

                    var numberOfBytesToRead = BitConverter.ToInt32(headerBuffer);
                    _lastBytePosition += EventStream.LengthOfEventHeader;

                    var ms = new MemoryStream(numberOfBytesToRead);

                    do {
                        Array.Clear(readBuffer);
                        readOffset = await fStream.ReadAsync(readBuffer, 0, Math.Min(numberOfBytesToRead, EventStream.LengthOfEventHeader));

                        if (readOffset > 0) {
                            ms.Write(readBuffer, 0, readOffset);
                        }

                        _lastBytePosition += readOffset;
                        numberOfBytesToRead -= readOffset;
                    } while (readOffset > 0);

                    ms.Seek(0, SeekOrigin.Begin);

                    var str = System.Text.Encoding.UTF8.GetString(ms.ToArray());

                    Current = _eventSerializer.Deserialize<StreamItem>(ms);
                }

                _reader._lastBytePosition = _lastBytePosition;
                return true;
            }
        }
    }
}