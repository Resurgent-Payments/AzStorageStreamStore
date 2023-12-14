namespace LvStreamStore;

using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class MemoryEventStream : EventStream {
    internal readonly SinglyLinkedList<StreamItem> _stream;
    public MemoryEventStream(ILoggerFactory loggerFactory, IOptions<MemoryEventStreamOptions> options)
        : base(loggerFactory, options.Value!) {
        _stream = new(options.Value.PageSize);
    }

    protected override Task WriteAsync(StreamItem item) {
        _stream.Append(item);
        return Task.CompletedTask;
    }

    private bool _disposed = false;
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (!disposing || _disposed) return;

        _disposed = true;
    }

    public override EventStreamReader GetReader() =>
        new MemoryEventStreamReader(this, (MemoryEventStreamOptions)_options);
}
