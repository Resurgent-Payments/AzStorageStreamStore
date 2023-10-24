namespace LvStreamStore;

using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class MemoryEventStream : EventStream {
    private readonly MemoryStream _stream = new();

    public MemoryEventStream(ILoggerFactory loggerFactory, IOptions<EventStreamOptions> options) : base(loggerFactory, options) {
        AfterConstructed();
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

    public override EventStreamReader GetReader() => new MemoryEventStreamReader(_stream, (MemoryEventStreamOptions)_options);
}
