namespace LvStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class MemoryEventStream : EventStream, IAsyncEnumerable<StreamItem> {
    private readonly MemoryStream _stream = new();
    private readonly MemoryEventStreamOptions _options;

    public MemoryEventStream(IOptions<EventStreamOptions> options) : base(options) {
        _options = options.Value as MemoryEventStreamOptions ?? new MemoryEventStreamOptions();
    }

    protected override async Task WriteAsync(byte[] data) {
        var endOfData = data.Length;

        for (var i = 0; i < data.Length; i++) {
            if (data[i] == 0x00) {
                endOfData = i;
                break;
            }
        }

        _stream.Seek(Checkpoint, SeekOrigin.Begin);
        await _stream.WriteAsync(data, 0, data.Length);

        Checkpoint += endOfData;
    }

    private bool _disposed = false;
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (!disposing || _disposed) return;

        _disposed = true;
    }

    public override IAsyncEnumerator<StreamItem> GetAsyncEnumerator(CancellationToken token = default) => new MemoryEventStreamReader(_stream, _options, token);
}
