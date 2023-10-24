namespace LvStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class MemoryEventStreamReader : EventStreamReader {
    private readonly MemoryStream _stream;
    private readonly MemoryEventStreamOptions _options;
    private long _lastPosition = 0;
    private int _lastOffset = 0;

    public MemoryEventStreamReader(MemoryStream stream, MemoryEventStreamOptions options) {
        _stream = stream;
        _options = options;
    }

    public override IAsyncEnumerator<StreamItem> GetAsyncEnumerator(CancellationToken token = default) =>
        new Enumerator(this, token);

    class Enumerator : IEnumerator {
        private readonly byte[] _buffer = new byte[4096];
        private MemoryEventStreamReader _reader;

        public StreamItem Current { get; private set; }

        public long Position { get; private set; }

        public int Offset { get; private set; }
        public Enumerator(MemoryEventStreamReader reader, CancellationToken? token = default) {
            Current = default;
            _reader = reader;
            Position = _reader._lastPosition;
            Offset = _reader._lastOffset;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync() {
            Position += Offset;
            Offset = 0;
            Current = default;

            var ms = new MemoryStream();

            int readOffset;

            var cPos = _reader._stream.Position;

            lock (_reader._stream) {
                try {
                    do {
                        _reader._stream.Seek(Position, SeekOrigin.Begin); // change to position+offset
                        Array.Clear(_buffer);
                        readOffset = _reader._stream.Read(_buffer, 0, _buffer.Length);

                        for (var idx = 0; idx < readOffset; idx++) {
                            if (_buffer[idx] == StreamConstants.NULL) break; // if null, then no further data exists.

                            if (_buffer[idx] == StreamConstants.EndOfRecord) { // found a point whereas we need to deserialize what we have in the buffer, yield it back to the caller, then advance the index by 1.
                                ms.Seek(0, SeekOrigin.Begin);

                                Current = JsonSerializer.Deserialize<StreamItem>(ms, _reader._options.JsonOptions)!;
                                Position += Offset + 1; // next iteration, the position will be at the beginning of the next item in the stream.
                                Offset = Convert.ToInt32(ms.Length);
                                return ValueTask.FromResult(true);
                            }

                            ms.WriteByte(_buffer[idx]);
                        }


                        Offset += readOffset; // need to do this in the event that we need to read 2x4k chunks, that we hold the position between the chunks.
                    } while (readOffset != 0);


                    _reader._lastPosition = Position;
                    _reader._lastOffset = Offset;

                    return ValueTask.FromResult(false);
                }
                finally {
                    ms?.Dispose();
                    _reader._stream.Seek(cPos, SeekOrigin.Begin);
                }
            }
        }
    }
}