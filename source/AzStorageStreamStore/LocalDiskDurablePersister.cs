namespace AzStorageStreamStore;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

public class SingleTenantkDurablePersister : IPersister {
    private readonly Channel<RecordedEvent> _storedEvents = Channel.CreateUnbounded<RecordedEvent>();
    private readonly Channel<ProcessWal> _writeWalToStream = Channel.CreateUnbounded<ProcessWal>();
    private readonly string _firstFileName = "chunk.dat";
    private readonly LocalDiskDurablePersisterOptions _options;

    private string _absoluteFileName => Path.Combine(_options.BaseDataPath, _firstFileName);

    public ChannelReader<RecordedEvent> AllStream => _storedEvents.Reader;
    public char record_separator = (char)0x1E; // record separator


    public SingleTenantkDurablePersister(IOptions<LocalDiskDurablePersisterOptions> options) {
        _options = options.Value;
        if (!Directory.Exists(options.Value.BaseDataPath)) {
            Directory.CreateDirectory(options.Value.BaseDataPath);
        }

        var firstFile = Path.Combine(options.Value.BaseDataPath, "chunk.dat");
        if (!File.Exists(firstFile)) {
            File.Create(firstFile, 1048576 * 256).Dispose(); // 256mb file block.  will never need this at this point.
        }
    }

    public IAsyncEnumerable<RecordedEvent> ReadAllAsync()
        => ReadAllAsync(0);

    public async IAsyncEnumerable<RecordedEvent> ReadAllAsync(long fromPosition) {
        var buffer = new byte[_options.FileReadBlockSize]; //typical block size
        var bytesRead = -1;
        var stringBuilder = new StringBuilder();

        using (var file = File.OpenRead(_absoluteFileName)) {
            int charsRead = file.Read(buffer, 0, buffer.Length);

            while (charsRead > 0) {
                int indexOfRecordSeparator;
                do {
                    if ((indexOfRecordSeparator = Array.IndexOf(buffer, record_separator)) > 0) {
                        stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, indexOfRecordSeparator - 1));
                        yield return JsonSerializer.Deserialize<RecordedEvent>(stringBuilder.ToString(), _options.JsonOptions);
                        stringBuilder.Clear();
                        continue;
                    }

                    if (indexOfRecordSeparator < 0 && bytesRead > 0) {
                        stringBuilder.Append(Encoding.ASCII.GetString(buffer));
                    }
                } while (indexOfRecordSeparator > -1);

                if (stringBuilder.Length > 0) {
                    yield return JsonSerializer.Deserialize<RecordedEvent>(stringBuilder.ToString(), _options.JsonOptions);
                }
            }
        }
    }

    public IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id)
        => ReadAsync(id, 0);

    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId id, long position) {
        await foreach (var @event in ReadAllAsync()) {
            if (@event.Key == id) yield return @event;
        }
    }

    public IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key)
        => ReadAsync(key, 0);

    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey key, long position) {
        await foreach (var @event in ReadAllAsync()) {
            if (@event.Key == key) yield return @event;
        }
    }

    public ValueTask<WriteResult> WriteAsync(StreamId id, ExpectedVersion version, EventData[] events) {

    }

    public void Dispose() {
        throw new NotImplementedException();
    }

}