namespace LvStreamStore;

using System.IO;
using System.Text.Json;
using System.Threading;

using Microsoft.Extensions.Options;

public class MemoryEventStreamObserver : EventStreamObserver {
    private readonly MemoryStream _data;
    private readonly MemoryEventStreamOptions _options;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly MemoryEventStream _parent;

    long _position;

    public MemoryEventStreamObserver(MemoryStream data, MemoryEventStream parent, IOptions<IEventStreamOptions> options, CancellationToken token = default) {
        _data = data;
        _parent = parent;
        _options = options.Value as MemoryEventStreamOptions ?? new MemoryEventStreamOptions();
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
        _position = 0;

        _ = token.Register(_cts.Cancel);
        _ = Task.Factory.StartNew(ObserveDataStream, _cts.Token);
    }

    bool _disposed = false;
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (!disposing || _disposed) return;

        _cts?.Dispose();

        _disposed = true;
    }

    private async Task ObserveDataStream() {
        do {
            byte[] buffer = new byte[4096];
            _data.Seek(_position, SeekOrigin.Begin);
            int bytesRead = _data.Read(buffer, (int)_position, buffer.Length);

            if (bytesRead > 0) {
                var ms = new MemoryStream();
                for (var idx = 0; idx < bytesRead; idx++) {
                    if (buffer[idx] == Constants.NULL) break;

                    if (buffer[idx] != Constants.EndOfRecord) {
                        ms.WriteByte(buffer[idx]);
                        continue;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    Publish(JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions)!);
                    _position += ms.Length + 1; // accounting for the end of record value.
                    ms?.Dispose();
                    ms = new MemoryStream();
                }
                if (ms.Length > 0) throw new InvalidOperationException("Should always be empty here.");
            }

            await _timer.WaitForNextTickAsync();
        } while (!_cts.IsCancellationRequested);
    }

    protected static class Constants {
        public static byte NULL = 0x00;
        public static byte EndOfRecord = 0x1E;
    }
}
