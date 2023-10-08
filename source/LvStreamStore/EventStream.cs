namespace LvStreamStore;

using System;
using System.IO;
using System.Reactive;
using System.Text.Json;
using System.Threading.Channels;

using Microsoft.Extensions.Options;

public abstract class EventStream : IObservable<StreamItem>, IDisposable {
    protected static class Constants {
        public static byte NULL = 0x00;
        public static byte EndOfRecord = 0x1E;
    }

    private readonly EventStreamOptions _options;
    private readonly Channel<WriteToStreamArgs> _streamWriter;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<IObserver<StreamItem>> _observers = new();
    private bool _disposed = false;

    protected ChannelReader<WriteToStreamArgs> StreamWriter => _streamWriter.Reader;

    public int Checkpoint { get; protected set; }

    public EventStream(IOptions<EventStreamOptions> options) {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _streamWriter = Channel.CreateUnbounded<WriteToStreamArgs>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });
        _cts.Token.Register(() => _streamWriter.Writer.Complete());
        Task.Factory.StartNew(StreamWriterImpl, _cts.Token);
    }

    /// <summary>
    /// Equivalent to a SubscribeToAll call.
    /// </summary>
    /// <param name="observer"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public IDisposable Subscribe(IObserver<StreamItem> observer) {
        _observers.Add(observer);
        return new StreamDisposer(() => _observers.Remove(observer));
    }

    public IDisposable SubscribeToStream(StreamId streamId, IObserver<StreamItem> observer) {
        return Subscribe(Observer.Create((StreamItem item) => {
            if (item.StreamId == streamId) {
                observer.OnNext(item);
            }
        }));
    }

    public async Task<IDisposable> SubscribeToStreamFromAsync(StreamId streamId, int revision, IObserver<StreamItem> observer) {
        var numberOfItemsRead = 1;

        // need to read to (hopefully) current.
        await foreach (var item in ReadFromAsync(streamId, revision)) {
            observer.OnNext(item);
            numberOfItemsRead += 1;
        }

        return Subscribe(Observer.Create((StreamItem item) => {
            if (item.StreamId == streamId) {
                if (numberOfItemsRead < revision && revision != int.MaxValue) {
                    numberOfItemsRead++;
                    return;
                }

                observer.OnNext(item);
            }
        }));
    }

    public IDisposable SubscribeToStream(StreamKey streamKey, IObserver<StreamItem> observer) {
        return Subscribe(Observer.Create((StreamItem item) => {
            if (streamKey == item.StreamId) {
                observer.OnNext(item);
            }
        }));
    }

    public async Task<IDisposable> SubscribeToStreamFromAsync(StreamKey streamKey, int revision, IObserver<StreamItem> observer) {
        var numberOfItemsRead = 1;

        return Subscribe(Observer.Create((StreamItem item) => {
            if (streamKey == item.StreamId) {
                if (numberOfItemsRead <= revision) {
                    numberOfItemsRead++;
                    return;
                }

                observer.OnNext(item);
            }
        }));
    }

    public IAsyncEnumerable<StreamItem> ReadAsync(StreamId streamId)
        => ReadFromAsync(streamId, 0);

    public IAsyncEnumerable<StreamItem> ReadAsync(StreamKey streamKey)
        => ReadFromAsync(streamKey, 0);

    public async IAsyncEnumerable<StreamItem> ReadFromAsync(StreamId streamId, int revision) {
        // full scan.
        if (await ReadAsync().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) throw new StreamDoesNotExistException();

        if (revision == int.MaxValue) yield break;

        // second full scan
        await foreach (var e in ReadAsync().OfType<RecordedEvent>().Where(s => s.StreamId == streamId).Skip(revision)) {
            yield return e;
        }
    }

    public async IAsyncEnumerable<StreamItem> ReadFromAsync(StreamKey streamKey, int revision) {
        if (revision == int.MaxValue) yield break;

        // full scan
        await foreach (var e in ReadAsync().OfType<RecordedEvent>().Where(s => streamKey == s.StreamId).Skip(revision)) {
            yield return e;
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
                if (await ReadAsync().OfType<StreamCreated>().AllAsync(sc => sc.StreamId != streamId)) {
                    // write the stream created event.
                    var ms = new MemoryStream();
                    var created = new StreamCreated(streamId);

                    JsonSerializer.Serialize(ms, created, _options.JsonOptions);
                    ms.WriteByte(Constants.EndOfRecord);
                    await WriteAsync(ms.ToArray());
                }

                // second full scan
                var revision = (await ReadAsync()
                    .OfType<RecordedEvent>()
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
                    ms.WriteByte(Constants.EndOfRecord);
                    await WriteAsync(ms.ToArray());
                    var eventOffset = Checkpoint - startIdx;
                    // todo: write startIdx and eventOffset to 'index'

                    foreach (var oberver in _observers) {
                        oberver.OnNext(recorded);
                    }
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

    protected abstract IAsyncEnumerable<StreamItem> ReadAsync();

    protected abstract Task WriteAsync(byte[] data);
}