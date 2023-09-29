namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class MemoryEventStream : EventStream {
    private readonly MemoryStream _stream = new();
    private readonly MemoryEventStreamOptions _options;

    public MemoryEventStream(IOptions<IEventStreamOptions> options) : base(options) {
        _options = (options.Value as MemoryEventStreamOptions) ?? new MemoryEventStreamOptions();
    }

    protected override async Task Persist(byte[] data) {
        int endOfData = data.Length;

        for (var i = 0; i < data.Length; i++) {
            if (data[i] == 0x00) {
                endOfData = i;
                break;
            }
        }

        var cPos = _stream.Position;
        _stream.Seek(Checkpoint, SeekOrigin.Begin);
        await _stream.WriteAsync(data, 0, data.Length);
        _stream.Seek(cPos, SeekOrigin.Begin);

        Checkpoint += endOfData;
    }

    protected override async IAsyncEnumerable<StreamItem> ReadLogAsync() {
        var buffer = new byte[4096];
        var ms = new MemoryStream();

        _stream.Seek(0, 0);
        int offset;
        do {
            Array.Clear(buffer);
            offset = await _stream.ReadAsync(buffer, 0, buffer.Length);

            for (var idx = 0; idx < offset; idx++) {
                if (buffer[idx] == Constants.NULL) break; // if null, then no further data exists.

                if (buffer[idx] == Constants.EndOfRecord) { // found a point whereas we need to deserialize what we have in the buffer, yield it back to the caller, then advance the index by 1.
                    ms.Seek(0, SeekOrigin.Begin);

                    yield return JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions)!;

                    ms?.Dispose();
                    ms = new MemoryStream();

                    continue;
                }

                ms.WriteByte(buffer[idx]);
            }
        } while (offset != 0);

        if (ms.Length > 0) {
            yield return JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions)!;
        }
    }
}
