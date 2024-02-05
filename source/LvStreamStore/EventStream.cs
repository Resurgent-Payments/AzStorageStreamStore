namespace LvStreamStore;

using System;
using System.Reactive;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

public abstract partial class EventStream : IDisposable {
    public const short LengthOfEventHeader = 4; // 32-bit integer.

    protected readonly EventStreamOptions _options;
    private readonly Channel<WriteToStreamArgs> _streamWriter;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;
    protected SinglyLinkedList<StreamMessage> Cache { get; private set; } = new();

    protected ILogger Log { get; }
    protected ChannelReader<WriteToStreamArgs> StreamReader => _streamWriter.Reader;

    public long Checkpoint { get; protected set; }

    protected EventStream(ILoggerFactory loggerFactory, EventStreamOptions options) {
        Log = loggerFactory.CreateLogger<EventStream>();
        _options = options;

        _streamWriter = Channel.CreateUnbounded<WriteToStreamArgs>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _cts.Token.Register(() => _streamWriter.Writer.Complete());
    }

    public virtual Task StartAsync() {
        MonitorForWriteRequests();
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId streamId, int? revision = null) {
        // need to find out if the stream exists.
        if (await GetReader().AllAsync(item => streamId != item.StreamId)) { throw new StreamDoesNotExistException(); }

        var numberOfItemsRead = 1;

        if (_options.UseCaching) {
            foreach (var item in Cache) {
                if (numberOfItemsRead <= revision && revision.HasValue) {
                    numberOfItemsRead++;
                    continue;
                }

                yield return (RecordedEvent)item!;
            }
        } else {
            await foreach (var item in GetReader()) {
                if (item.GetType().IsAssignableTo(typeof(RecordedEvent)) && streamId == item.StreamId) {
                    if (numberOfItemsRead <= revision && revision.HasValue) {
                        numberOfItemsRead++;
                        continue;
                    }

                    yield return (RecordedEvent)item!;
                }
            }
        }
    }

    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey streamKey, int? revision = null) {
        var numberOfItemsRead = 1;

        if (_options.UseCaching) {
            foreach (var item in Cache) {
                if (item.GetType().IsAssignableTo(typeof(RecordedEvent)) && streamKey == item.StreamId) {
                    if (numberOfItemsRead <= revision && revision.HasValue) {
                        numberOfItemsRead++;
                        continue;
                    }

                    yield return (RecordedEvent)item!;
                }
            }
        } else {
            await foreach (var item in GetReader()) {
                if (item.GetType().IsAssignableTo(typeof(RecordedEvent)) && streamKey == item.StreamId) {
                    if (numberOfItemsRead <= revision && revision.HasValue) {
                        numberOfItemsRead++;
                        continue;
                    }

                    yield return (RecordedEvent)item!;
                }
            }
        }
    }

    public async ValueTask<WriteResult> AppendAsync(StreamId streamId, ExpectedVersion version, IEnumerable<EventData> events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _streamWriter.Writer.WriteAsync(new WriteToStreamArgs(tcs, streamId, version, events));
        return await tcs.Task;
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposing || _disposed) return;
        _cts.Cancel();
        _disposed = true;
    }

    public abstract EventStreamReader GetReader();

    protected async void MonitorForWriteRequests() {
        await Task.Yield();

        try {
            while (await StreamReader.WaitToReadAsync(_cts.Token)) {
                var possible = await StreamReader.ReadAsync(_cts.Token);
                var onceCompleted = possible.OnceCompleted;
                var streamId = possible.Id;
                var expected = possible.Version;
                var events = possible.Events;

                try {
                    if (!await PassesValidationAsync(onceCompleted, streamId, expected, events)) continue;


                    // refactor to create one memorystream to be appended to a WAL.
                    // get last stream position.
                    var position = _options.UseCaching
                        ? Cache.LastOrDefault()?.Position ?? 0
                        : (await GetReader().LastOrDefaultAsync())?.Position ?? 0;

                    // first full scan
                    // if we don't have a StreamCreated event, we need to append one now.
                    if (_options.UseCaching
                        ? Cache.OfType<StreamCreated>().All(sc => sc.StreamId != streamId)
                        : await GetReader().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) {
                        position += 1;
                        var created = new StreamCreated(streamId, position);

                        await WriteAsync(created);
                    }

                    // second full scan
                    var written = _options.UseCaching
                        ? Cache.OfType<RecordedEvent>()
                            .Where(recorded => recorded.StreamId == streamId)
                            .ToList()
                        : await GetReader()
                            .OfType<RecordedEvent>()
                            .Where(recorded => recorded.StreamId == streamId)
                            .ToListAsync();

                    var recorded = events.Where(e => written.All(w => e.EventId != w.EventId))
                        .Select(e => {
                            position += 1;
                            return new RecordedEvent(streamId, e.EventId, position, e.Type, e.Metadata, e.Data);

                        })
                        .ToArray();

                    await WriteAsync(recorded);

                    onceCompleted.SetResult(WriteResult.Ok(position));
                }
                catch (Exception exc) {
                    Log.LogWarning(exc, "Write process failed.");
                    onceCompleted.SetResult(WriteResult.Failed(-1, exc));
                }
            }
        }
        catch (OperationCanceledException) {
            //no-op - token is probably marked as canceled, so we should just exit.
        }
    }

    protected virtual async ValueTask<bool> PassesValidationAsync(TaskCompletionSource<WriteResult> onceCompleted, StreamId streamId, ExpectedVersion expected, IEnumerable<EventData> events) {
        if (!events.Any()) { return true; }

        try {
            switch (expected) {
                case -4: // stream exists
                    if (!await ReadAsync(StreamKey.All).AllAsync(e => e.StreamId != streamId)) {
                        Log.LogWarning("An attempt to write events to stream {@streamId} failed.  Stream does not exist, and was expected to.", streamId);
                        onceCompleted.SetResult(WriteResult.Failed(-1, new StreamDoesNotExistException()));
                        return false;
                    }

                    break;
                case -2: // any stream
                    break;
                case -1: // no stream
                    if (!await ReadAsync(StreamKey.All).AllAsync(s => s.StreamId != streamId)) {
                        Log.LogWarning("An attempt to write events to stream {@streamId} failed.  Stream exists, and was not expected to.", streamId);
                        onceCompleted.SetResult(WriteResult.Failed(-1, new WrongExpectedVersionException(ExpectedVersion.NoStream, ExpectedVersion.StreamExists)));
                        return false;
                    }

                    break;
                default:
                    var filtered = await ReadAsync(StreamKey.All).OfType<RecordedEvent>().Where(e => e.StreamId == streamId).ToListAsync();

                    if (!filtered.Any()) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, new WrongExpectedVersionException(expected, ExpectedVersion.NoStream)));
                        return false;
                    }

                    if (filtered.Count() != expected) {
                        // if all events are appended, considered as a double request and post-back ok.
                        if (events.All(e => filtered.All(i => i.EventId != e.EventId))) {
                            Log.LogTrace("Attempt to write events is considered as successful.  All events have been recorded.");
                            onceCompleted.SetResult(WriteResult.Ok(filtered.Max(x => x.Position)));
                            return false;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => filtered.All(s => s.EventId != e.EventId)).Any()) {
                            Log.LogTrace("Attempt to write events is considered as failed.  All or some events have previously been recorded.");
                            onceCompleted.SetResult(WriteResult.Failed(filtered.Max(x => x.Position),
                                new WrongExpectedVersionException(expected, filtered.LastOrDefault()?.Position ?? ExpectedVersion.NoStream)));
                            return false;
                        }
                    }

                    break;
            }
        }
        catch (Exception exc) {
            Log.LogError(exc, "Stream validation failed.");
            // todo: log?
            return false;
        }

        return true;
    }

    protected abstract Task WriteAsync(params StreamMessage[] item);
}