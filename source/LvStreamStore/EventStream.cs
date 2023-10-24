namespace LvStreamStore;

using System;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public abstract partial class EventStream : IDisposable {
    private readonly ILoggerFactory _loggerFactory;
    protected readonly EventStreamOptions _options;
    private readonly Channel<WriteToStreamArgs> _streamWriter;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;
    private Subscribers _subscribers;
    private Bus _inboundEventBus;

    protected ChannelReader<WriteToStreamArgs> StreamWriter => _streamWriter.Reader;

    public int Checkpoint { get; protected set; }

    public EventStream(ILoggerFactory loggerFactory, IOptions<EventStreamOptions> options) {
        _loggerFactory = loggerFactory;
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _streamWriter = Channel.CreateUnbounded<WriteToStreamArgs>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        _cts.Token.Register(() => _streamWriter.Writer.Complete());
        Task.Factory.StartNew(StreamWriterImpl, _cts.Token);
    }

    protected void AfterConstructed() {
        _inboundEventBus = new(_loggerFactory.CreateLogger(GetType()));
        _subscribers = new(this, _loggerFactory.CreateLogger(GetType()));
    }

    public async Task<IDisposable> SubscribeToStreamAsync(Func<RecordedEvent, Task> onAppeared)
        => await _subscribers.SubscribeToStreamAsync(onAppeared);

    public async Task<IDisposable> SubscribeToStreamAsync(StreamId streamId, Func<RecordedEvent, Task> onAppeared) {
        return await _subscribers.SubscribeToStreamAsync(async (recorded) => {
            if (streamId == recorded.StreamId) {
                await onAppeared(recorded);
            }
        });
    }

    public async Task<IDisposable> SubscribeToStreamAsync(StreamKey streamKey, Func<RecordedEvent, Task> onAppeared) {
        return await _subscribers.SubscribeToStreamAsync(async (recorded) => {
            if (streamKey == recorded.StreamId) {
                await onAppeared(recorded);
            }
        });
    }

    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamId streamId, int? revision = null) {
        // need to find out if the stream exists.
        if (await GetReader().AllAsync(item => streamId != item.StreamId)) { throw new StreamDoesNotExistException(); }

        var numberOfItemsRead = 1;

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

    public async IAsyncEnumerable<RecordedEvent> ReadAsync(StreamKey streamKey, int? revision = null) {
        var numberOfItemsRead = 1;

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

    public async ValueTask<WriteResult> AppendAsync(StreamId streamId, ExpectedVersion version, IEnumerable<EventData> events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        await _streamWriter.Writer.WriteAsync(new WriteToStreamArgs(tcs, streamId, version, events));
        return await tcs.Task;
    }

    public void Dispose() {
        Dispose(true);
        _subscribers?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposing || _disposed) return;
        _cts.Cancel();
        _disposed = true;
    }

    public abstract EventStreamReader GetReader();

    protected async Task StreamWriterImpl() {
        await foreach (var posssibleWalEntry in StreamWriter.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expected = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            try {
                if (!await PassesValidationAsync(onceCompleted, streamId, expected, events)) continue;


                // refactor to create one memorystream to be appended to a WAL.
                // get last stream position.
                var position = (await GetReader().LastOrDefaultAsync())?.Position ?? 0;

                // first full scan
                // if we don't have a StreamCreated event, we need to append one now.
                if (await GetReader().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) {
                    // write the stream created event.
                    var ms = new MemoryStream();
                    position += 1;
                    var created = new StreamCreated(streamId, position);

                    JsonSerializer.Serialize(ms, created, _options.JsonOptions);
                    ms.WriteByte(StreamConstants.EndOfRecord);
                    await WriteAsync(ms.ToArray());
                }

                // cache the current checkpoint.

                foreach (var @event in events) {
                    position += 1;
                    var recorded = new RecordedEvent(streamId, @event.EventId, position, @event.Type, @event.Metadata, @event.Data);

                    // write the stream created event.
                    var ms = new MemoryStream();

                    var startIdx = Checkpoint;
                    JsonSerializer.Serialize(ms, recorded, _options.JsonOptions);
                    ms.WriteByte(StreamConstants.EndOfRecord);
                    await WriteAsync(ms.ToArray());
                    var eventOffset = Checkpoint - startIdx;
                    // todo: write startIdx and eventOffset to 'index'

                    await _inboundEventBus.PublishAsync(recorded); //note: this is going to be really f** slow.
                }

                // capture the offset here.

                // write the index log entry.


                // dump WAL to disc
                // - - - - - 
                // emit message to append WAL to data file.
                //  - capture last-modified for data file, send to wal writer
                //  - if wal writer finds last-modified changed, then reject write request.

                onceCompleted.SetResult(WriteResult.Ok(position));
            }
            catch (Exception exc) {
                onceCompleted.SetResult(WriteResult.Failed(-1, exc));
            }
        }
    }

    protected virtual async ValueTask<bool> PassesValidationAsync(TaskCompletionSource<WriteResult> onceCompleted, StreamId streamId, ExpectedVersion expected, IEnumerable<EventData> events) {
        try {
            switch (expected) {
                case -4: // stream exists
                    if (!await ReadAsync(StreamKey.All).AllAsync(e => e.StreamId != streamId)) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, new StreamDoesNotExistException()));
                        return false;
                    }

                    break;
                case -2: // any stream
                    break;
                case -1: // no stream
                    if (!await ReadAsync(StreamKey.All).AllAsync(s => s.StreamId != streamId)) {
                        var revision = ReadAsync(StreamKey.All).OfType<RecordedEvent>().MaxAsync(e => e.Position);
                        onceCompleted.SetResult(WriteResult.Failed(-1, new WrongExpectedVersionException(ExpectedVersion.NoStream, ExpectedVersion.StreamExists)));
                        return false;
                    }

                    //else {
                    //    // check for duplicates here.
                    //    var nonEmptyStreamEvents = await ReadAsync(StreamKey.All).OfType<RecordedEvent>().Where(s => s.StreamId == streamId).ToListAsync();

                    //    if (nonEmptyStreamEvents.Any()) {
                    //        // if all events are appended, considered as a double request and post-back ok.
                    //        if (!nonEmptyStreamEvents.All(e => events.All(i => e.EventId != i.EventId))) {
                    //            onceCompleted.SetResult(WriteResult.Ok(nonEmptyStreamEvents.Max(x => x.Revision)));
                    //            return false;
                    //        }
                    //    }
                    //}

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
                            onceCompleted.SetResult(WriteResult.Ok(filtered.Max(x => x.Position)));
                            return false;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => filtered.All(s => s.EventId != e.EventId)).Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(filtered.Max(x => x.Position),
                                new WrongExpectedVersionException(expected, filtered.LastOrDefault()?.Position ?? ExpectedVersion.NoStream)));
                            return false;
                        }
                    }

                    break;
            }
        }
        catch (Exception) {
            // todo: log?
            return false;
        }

        return true;
    }

    protected abstract Task WriteAsync(byte[] data);
}