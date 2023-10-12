using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LvStreamStore.LocalStorage")]

namespace LvStreamStore;

using System;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public abstract class EventStream : IDisposable {
    private readonly EventStreamOptions _options;
    private readonly Channel<WriteToStreamArgs> _streamWriter;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    private InMemoryBus _inboundEventBus;
    private readonly List<IDisposable> _subscriptions = new();

    protected ChannelReader<WriteToStreamArgs> StreamWriter => _streamWriter.Reader;

    public int Checkpoint { get; protected set; }

    public EventStream(ILoggerFactory loggerFactory, IOptions<EventStreamOptions> options) {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _streamWriter = Channel.CreateUnbounded<WriteToStreamArgs>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _cts.Token.Register(() => _streamWriter.Writer.Complete());
        Task.Factory.StartNew(StreamWriterImpl, _cts.Token);


        _inboundEventBus = new InMemoryBus();
    }

    public IDisposable SubscribeToStream(Func<RecordedEvent, Task> onAppeared) {
        var subscriber = new EventStreamSubscriber(_inboundEventBus, onAppeared).Start(StreamKey.All);
        _subscriptions.Add(subscriber);
        return subscriber;
    }

    public IDisposable SubscribeToStream(StreamId streamId, Func<RecordedEvent, Task> onAppeared) {
        var subscriber = new EventStreamSubscriber(_inboundEventBus, onAppeared).Start(streamId);
        _subscriptions.Add(subscriber);
        return subscriber;
    }

    public IDisposable SubscribeToStream(StreamKey streamKey, Func<RecordedEvent, Task> onAppeared) {
        var subscriber = new EventStreamSubscriber(_inboundEventBus, onAppeared).Start(streamKey);
        _subscriptions.Add(subscriber);
        return subscriber;
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
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) {
        if (!disposing || _disposed) return;
        _cts.Cancel();
        _disposed = true;
    }

    protected abstract EventStreamReader GetReader();

    protected async Task StreamWriterImpl() {
        await foreach (var posssibleWalEntry in StreamWriter.ReadAllAsync()) {
            var onceCompleted = posssibleWalEntry.OnceCompleted;
            var streamId = posssibleWalEntry.Id;
            var expected = posssibleWalEntry.Version;
            var events = posssibleWalEntry.Events;

            try {
                if (!await PassesValidationAsync(onceCompleted, streamId, expected, events)) continue;


                // refactor to create one memorystream to be appended to a WAL.

                // first full scan
                // if we don't have a StreamCreated event, we need to append one now.
                if (await GetReader().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) {
                    // write the stream created event.
                    var ms = new MemoryStream();
                    var created = new StreamCreated(streamId);

                    JsonSerializer.Serialize(ms, created, _options.JsonOptions);
                    ms.WriteByte(StreamConstants.EndOfRecord);
                    await WriteAsync(ms.ToArray());
                }

                // second full scan
                var revision = (await GetReader().OfType<RecordedEvent>()
                    .Where(e => e.StreamId == streamId)
                    .LastOrDefaultAsync())?.Revision ?? -1L;

                // cache the current checkpoint.

                foreach (var @event in events) {
                    revision += 1;
                    var recorded = new RecordedEvent(streamId, @event.EventId, revision, @event.Type, @event.Metadata, @event.Data);

                    // write the stream created event.
                    var ms = new MemoryStream();

                    var startIdx = Checkpoint;
                    JsonSerializer.Serialize(ms, recorded, _options.JsonOptions);
                    ms.WriteByte(StreamConstants.EndOfRecord);
                    await WriteAsync(ms.ToArray());
                    var eventOffset = Checkpoint - startIdx;
                    // todo: write startIdx and eventOffset to 'index'

                    await _inboundEventBus.PublishAsync(new EventRecorded(recorded)); //note: this is going to be really f** slow.
                }

                // capture the offset here.

                // write the index log entry.


                // dump WAL to disc
                // - - - - - 
                // emit message to append WAL to data file.
                //  - capture last-modified for data file, send to wal writer
                //  - if wal writer finds last-modified changed, then reject write request.

                onceCompleted.SetResult(WriteResult.Ok(revision));
            }
            catch (Exception exc) {
                onceCompleted.SetResult(WriteResult.Failed(-1, exc));
            }
        }
    }

    protected virtual async ValueTask<bool> PassesValidationAsync(TaskCompletionSource<WriteResult> onceCompleted, StreamId streamId, ExpectedVersion expected, IEnumerable<EventData> events) {
        try {
            switch (expected) {
                case -3: // no stream
                    if (!await ReadAsync(StreamKey.All).AllAsync(e => e.StreamId != streamId)) {
                        onceCompleted.SetResult(WriteResult.Failed(-1, new StreamExistsException()));
                        return false;
                    }

                    break;
                case -2: // any stream
                    break;
                case -1: // empty stream
                    if (await ReadAsync(StreamKey.All).AllAsync(s => s.StreamId != streamId)) {
                        var revision = ReadAsync(StreamKey.All).OfType<RecordedEvent>().MaxAsync(e => e.Revision);
                        onceCompleted.SetResult(WriteResult.Failed(-1, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, -1)));
                        return false;
                    } else {
                        // check for duplicates here.
                        var nonEmptyStreamEvents = await ReadAsync(StreamKey.All).OfType<RecordedEvent>().Where(s => s.StreamId == streamId).ToListAsync();

                        if (nonEmptyStreamEvents.Any()) {
                            // if all events are appended, considered as a double request and post-back ok.
                            if (!nonEmptyStreamEvents.All(e => events.All(i => e.EventId != i.EventId))) {
                                onceCompleted.SetResult(WriteResult.Ok(nonEmptyStreamEvents.Max(x => x.Revision)));
                                return false;
                            }
                        }
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
                            onceCompleted.SetResult(WriteResult.Ok(filtered.Max(x => x.Revision)));
                            return false;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => filtered.All(s => s.EventId != e.EventId)).Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(filtered.Max(x => x.Revision),
                                new WrongExpectedVersionException(expected, filtered.LastOrDefault()?.Revision ?? ExpectedVersion.NoStream)));
                            return false;
                        }
                    }

                    break;
            }
        }
        catch (Exception exc) {
            return false;
        }

        return true;
    }

    protected abstract Task WriteAsync(byte[] data);
}