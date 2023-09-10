namespace AzStorageStreamStore;

using System.Collections.Concurrent;
using System.Threading.Channels;

public class InMemoryStoreClient : IStoreClient {
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();

    //$all stream
    private readonly SinglyLinkedList<RecordedEvent> _allStream = new();

    //[object] stream
    private readonly Dictionary<StreamId, SinglyLinkedList<LinkTo>> _idStreams = new();

    //$ce- streams
    private readonly Dictionary<StreamKey, SinglyLinkedList<LinkTo>> _categories = new();

    // queues the recorded events for publication to handlers
    private readonly Channel<RecordedEvent> _publisher;

    // performs the recording of events to the given stream.
    private readonly Channel<Work> _eventWriter;

    // holds the task that manages pumping events from the _publisher stream to all handlers.
    private Task _pumpEventsToSubscribers;

    // can this be done via TPL?
    private ConcurrentBag<Action<RecordedEvent>> _subscriptions = new();
    private readonly ConcurrentDictionary<StreamKey, ConcurrentBag<Action<RecordedEvent>>> _streamSubscriptions = new();

    private long _position = -1;

    private Task _streamWriterTask;

    public InMemoryStoreClient() {
        _eventWriter = Channel.CreateUnbounded<Work>(new UnboundedChannelOptions {
            AllowSynchronousContinuations = false,
            SingleWriter = false,
            SingleReader = false
        });
        _streamWriterTask = Task.Factory.StartNew(RecordEventsImplAsync);
        _publisher = Channel.CreateUnbounded<RecordedEvent>(new UnboundedChannelOptions {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
        _cts.Token.Register(() => {
            _publisher.Writer.Complete();
            _pumpEventsToSubscribers?.Dispose();
            _streamWriterTask?.Dispose();
        });
    }

    public Task InitializeAsync() {
        _pumpEventsToSubscribers = Task.Factory.StartNew(MessagePump, _cts.Token);
        return Task.CompletedTask;
    }

    public async ValueTask<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events) {
        var tcs = new TaskCompletionSource<WriteResult>();
        var workToPerform = new Work(key, tcs, version, events);
        await _eventWriter.Writer.WriteAsync(workToPerform);
        return await tcs.Task;
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously - we need to have async here for respecting the interface signature.
    public async IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key) {
        if (_categories.TryGetValue(key, out var stream)) {
            //todo: switch to a foreach?
            var enumerator = stream.GetEnumerator();

            while (enumerator.MoveNext())
                yield return enumerator.Current.Event;
        }
    }

    public async IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamId id) {
        if (!_idStreams.TryGetValue(id, out var stream)) throw new StreamDoesNotExistException();

        foreach (var item in stream) {
            yield return item.Event;
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously - we need to have async here for respecting the interface signature.

    public IDisposable SubscribeToAll(Action<RecordedEvent> eventHandler) {
        _subscriptions.Add(eventHandler);
        return new StreamDisposer(() => Interlocked.Exchange(ref _subscriptions, new ConcurrentBag<Action<RecordedEvent>>(_subscriptions.Except(new[] { eventHandler }))));
    }

    public IDisposable SubscribeToAllFrom(long position, Action<RecordedEvent> eventHandler) {
        foreach (var e in _allStream.Skip(Convert.ToInt32(position))) {
            eventHandler.Invoke(e);
        }
        _subscriptions.Add(eventHandler);
        return new StreamDisposer(() => Interlocked.Exchange(ref _subscriptions, new ConcurrentBag<Action<RecordedEvent>>(_subscriptions.Except(new[] { eventHandler }))));
    }

    public IDisposable SubscribeToStream(StreamKey key, Action<RecordedEvent> eventHandler) {
        // build subscription for specific stream.
        if (!_streamSubscriptions.TryGetValue(key, out var bag)) {
            bag = new();
            _streamSubscriptions.TryAdd(key, bag);
        }

        if (_categories.TryGetValue(key, out var stream)) {
            foreach (var link in stream) {
                eventHandler.Invoke(link.Event);
            }
        }

        bag.Add(eventHandler);

        return new StreamDisposer(() =>
            Interlocked.Exchange(
                ref bag,
                new ConcurrentBag<Action<RecordedEvent>>(bag.Except(new[] { eventHandler }))
            )
        );
    }

    public IDisposable SubscribeToStreamFrom(long position, StreamKey key, Action<RecordedEvent> eventHandler) {
        // build subscription for specific stream.
        if (!_streamSubscriptions.TryGetValue(key, out var bag)) {
            bag = new();
            _streamSubscriptions.TryAdd(key, bag);
        }

        if (_categories.TryGetValue(key, out var stream)) {
            foreach (var linkTo in stream.Skip(Convert.ToInt32(position))) {
                eventHandler.Invoke(linkTo.Event);
            }
        }

        bag.Add(eventHandler);

        return new StreamDisposer(() =>
            Interlocked.Exchange(
                ref bag,
                new ConcurrentBag<Action<RecordedEvent>>(bag.Except(new[] { eventHandler }))
            )
        );
    }

    private async Task RecordEventsImplAsync() {
        await foreach (var toWrite in _eventWriter.Reader.ReadAllAsync()) {
            var tcs = toWrite.CompletionSource;

            // check e-tag.. if not equivalent to what we know, reload?

            switch (toWrite.Version) {
                case -3: // no stream
                    if (_idStreams.TryGetValue(toWrite.Id, out var noStream)) {
                        tcs.SetResult(WriteResult.Failed(_position, noStream.Last().Revision, new StreamExistsException()));
                        continue;
                    }
                    break;
                case -2: // any stream
                    break;
                case -1: // empty stream

                    if (!_idStreams.TryGetValue(toWrite.Id, out var emptyStream)) {
                        tcs.SetResult(WriteResult.Failed(-1, ExpectedVersion.NoStream, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, ExpectedVersion.NoStream)));
                        continue;
                    }

                    if (emptyStream.Length != 0) {
                        tcs.SetResult(WriteResult.Failed(_position, -1, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, emptyStream.Length)));
                        continue;
                    }

                    break;
                default:
                    if (!_idStreams.TryGetValue(toWrite.Id, out var possibleStream)) {
                        tcs.SetResult(WriteResult.Failed(_position, ExpectedVersion.NoStream, new WrongExpectedVersionException(toWrite.Version, ExpectedVersion.NoStream)));
                    }

                    if (possibleStream != null && possibleStream.Length != toWrite.Version) {
                        var existingEventIds = possibleStream.Select(ps => ps.Event.EventId).ToArray();

                        // if all events are appended, considered as a double request and post-back ok.
                        if (toWrite.Events.All(e => existingEventIds.Contains(e.EventId))) {
                            tcs.SetResult(WriteResult.Ok(_position, possibleStream.Last().Revision));
                            continue;
                        }

                        // if all events were not appended -- or -- only some were appended, then throw a wrong expected version.
                        if (toWrite.Events.All(e => !existingEventIds.Contains(e.EventId)) || toWrite.Events.Any(e => existingEventIds.Contains(e.EventId))) {
                            tcs.SetResult(WriteResult.Failed(_position, possibleStream.Last().Revision, new WrongExpectedVersionException(toWrite.Version, possibleStream.Length)));
                            continue;
                        }
                    }
                    break;
            }

            // append events.
            foreach (var e in toWrite.Events.Select(x => new RecordedEvent(x.Key, x.EventId, _position++, x.Data))) {
                if (!_idStreams.TryGetValue(e.Key, out var idStream)) {
                    idStream = new();
                    _idStreams.Add(e.Key, idStream);
                }
                _allStream.Append(e);
                idStream.Append(new(idStream.Length, e));

                // need to walk through the key and create all possible permutations.
                foreach (var projection in (StreamKey)toWrite.Id) {
                    if (!_categories.TryGetValue(projection, out var linkTos)) {
                        linkTos = new();
                        _categories.Add(projection, linkTos);
                    }

                    linkTos.Append(new(linkTos.Length, e));
                }

                await _publisher.Writer.WriteAsync(e);
            }

            var lastRevision = _idStreams[toWrite.Id].Last().Revision;
            tcs.SetResult(WriteResult.Ok(_position, lastRevision));
        }
    }

    private bool _hasBeenDisposed = false;
    public void Dispose() => Dispose(true);

    protected virtual void Dispose(bool disposing) {
        if (_hasBeenDisposed || !disposing) return;

        _hasBeenDisposed = true;
    }

    private async void MessagePump() {
        while (!_cts.IsCancellationRequested) {
            await foreach (var e in _publisher.Reader.ReadAllAsync(_cts.Token)) {
                foreach (var allAction in _subscriptions) {
                    allAction.Invoke(e);
                }

                var keys = (StreamKey)e.Key;

                foreach (var key in keys) {
                    if (!_streamSubscriptions.TryGetValue(key, out var bag)) continue;
                    foreach (var act in bag) {
                        act.Invoke(e);
                    }
                }
            }
        }
    }

    private record Work(StreamId Id, TaskCompletionSource<WriteResult> CompletionSource, ExpectedVersion Version, EventData[] Events);
}
