namespace LvStreamStore;

using System.IO;
using System.Text.Json;
using System.Threading;

using Microsoft.Extensions.Options;

public class LocalStorageEventStreamObserver : EventStreamObserver {
    private readonly CancellationTokenSource _cts = new();
    LocalStorageEventStreamOptions _options;
    private readonly PeriodicTimer _timer;
    private readonly string _dataFile;
    private long _position;
    private EventStream _parent;

    public LocalStorageEventStreamObserver(IOptions<IEventStreamOptions> options, EventStream parent) {
        _options = options.Value as LocalStorageEventStreamOptions ?? new LocalStorageEventStreamOptions();
        _parent = parent;
        _dataFile = Path.Combine(_options.BaseDataPath, "chunk.dat");

        // get current length of file... should note where to start observing _from_.
        _position = new FileInfo(_dataFile).Length;

        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
        _ = Task.Factory.StartNew(Observe, _cts.Token);
    }

    private async Task Observe() {
        var buffer = new byte[4096];
        var ms = new MemoryStream();

        using (var fStream = new FileStream(_dataFile, new FileStreamOptions { Access = FileAccess.Read, Mode = FileMode.Open, Options = FileOptions.Asynchronous, Share = FileShare.ReadWrite })) {
            do {
                await _timer.WaitForNextTickAsync();

                Array.Clear(buffer);
                int offset = 0;

                do {
                    offset = await fStream.ReadAsync(buffer, 0, buffer.Length);

                    if ((_position + offset) <= _parent.Checkpoint) {
                        for (var idx = 0; idx < offset; idx++) {
                            if (buffer[idx] == Constants.NULL) break; // if null, then no further data exists.

                            if (buffer[idx] == Constants.EndOfRecord) { // found a point whereas we need to deserialize what we have in the buffer, yield it back to the caller, then advance the index by 1.
                                ms.Seek(0, SeekOrigin.Begin);

                                Publish(JsonSerializer.Deserialize<StreamItem>(ms, _options.JsonOptions)!);

                                _position += ms.Length + 1; // need to take into account the endofrecord value.

                                ms?.Dispose();
                                ms = new MemoryStream();

                                continue;
                            }

                            ms.WriteByte(buffer[idx]);
                        }
                    }


                } while (offset > 0);
            } while (!_cts.IsCancellationRequested);
        }
    }

    bool _disposed = false;
    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        if (!disposing || _disposed) return;

        _cts.Dispose();
        _timer.Dispose();

        _disposed = true;
    }

    protected static class Constants {
        public static byte NULL = 0x00;
        public static byte EndOfRecord = 0x1E;
    }
}