namespace LvStreamStore;

using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class MemoryEventStreamReader : EventStreamReader {
    private readonly MemoryStream _stream;
    private readonly MemoryEventStreamOptions _options;
    private readonly CancellationToken _token;
    private readonly byte[] _buffer = new byte[4096];

    public StreamItem Current { get; private set; }

    public int Position { get; private set; }

    public int Offset { get; private set; }

    public MemoryEventStreamReader(MemoryStream stream, MemoryEventStreamOptions options, CancellationToken token = default) {
        _stream = stream;
        _options = options;
        _token = token;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public ValueTask<bool> MoveNextAsync() {
        Position += Offset; 
        Offset = 0;
        Current = default;

        var ms = new MemoryStream();

        int readOffset;

        var cPos = _stream.Position;

        try {
            do {
                _stream.Seek(Position, SeekOrigin.Begin); // change to position+offset
                Array.Clear(_buffer);
                readOffset = _stream.Read(_buffer, 0, _buffer.Length);

                for (var idx = 0; idx < readOffset; idx++) {
                    if (_buffer[idx] == StreamConstants.NULL) break; // if null, then no further data exists.

                    if (_buffer[idx] == StreamConstants.EndOfRecord) { // found a point whereas we need to deserialize what we have in the buffer, yield it back to the caller, then advance the index by 1.
                        ms.Seek(0, SeekOrigin.Begin);

                        Current = JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions)!;
                        Position += Offset += 1; // next iteration, the position will be at the beginning of the next item in the stream.
                        Offset = Convert.ToInt32(ms.Length);
                        return ValueTask.FromResult(true);
                    }

                    ms.WriteByte(_buffer[idx]);
                }


                Offset += _buffer.Length; // need to do this in the event that we need to read 2x4k chunks, that we hold the position between the chunks.
            } while (readOffset != 0);

            return ValueTask.FromResult(false);
        }
        finally {
            ms?.Dispose();
            _stream.Seek(cPos, SeekOrigin.Begin);
        }
    }
}