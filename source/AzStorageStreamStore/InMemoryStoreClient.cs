namespace AzStorageStreamStore;

using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

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
    private readonly TransformBlock<Work, WriteResult> _recordEvents;

    // holds the task that manages pumping events from the _publisher stream to all handlers.
    private Task _pumpEventsToSubscribers;

    // can this be done via TPL?
    private readonly List<Action<RecordedEvent>> _subscriptions = new();
    private readonly ConcurrentDictionary<StreamKey, ConcurrentBag<Action<RecordedEvent>>> _streamSubscriptions = new();

    private long _position = -1;

    public InMemoryStoreClient() {
        _publisher = Channel.CreateUnbounded<RecordedEvent>(new UnboundedChannelOptions {
            AllowSynchronousContinuations = false,
            SingleReader = true,
            SingleWriter = true
        });
        _recordEvents = new TransformBlock<Work, WriteResult>(RecordEventsImplAsync);
        _cts.Token.Register(() => {
            _recordEvents.Complete();
            _publisher.Writer.Complete();
            _pumpEventsToSubscribers?.Dispose();
        });
    }

    public Task InitializeAsync() {
        _pumpEventsToSubscribers = Task.Factory.StartNew(MessagePump, _cts.Token);
        return Task.CompletedTask;
    }

    public async Task<WriteResult> AppendToStreamAsync(StreamId key, ExpectedVersion version, params EventData[] events) {
        if (await _recordEvents.SendAsync(new Work(key, version, events))) {
            return await _recordEvents.ReceiveAsync();
        }

        throw new InvalidOperationException("Repository is closing or has been closed.");
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously - we need to have async here for respecting the interface signature.
    public async IAsyncEnumerable<RecordedEvent> ReadStreamAsync(StreamKey key) {
        if (_categories.TryGetValue(key, out var stream)) {
            var enumerator = stream.GetEnumerator();

            while (enumerator.MoveNext())
                yield return enumerator.Current.Event;
        }
    }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously - we need to have async here for respecting the interface signature.

    public void SubscribeToAll(Action<RecordedEvent> eventHandler) {
        _subscriptions.Add(eventHandler);
    }

    public void SubscribeToAllFrom(long position, Action<RecordedEvent> eventHandler) {
        foreach (var e in _allStream.Skip(Convert.ToInt32(position))) {
            eventHandler.Invoke(e);
        }
        _subscriptions.Add(eventHandler);
    }

    public void SubscribeToStream(StreamKey key, Action<RecordedEvent> eventHandler) {
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
    }

    public void SubscribeToStreamFrom(long position, StreamKey key, Action<RecordedEvent> eventHandler) {
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
    }

    private async Task<WriteResult> RecordEventsImplAsync(Work toWrite) {
        // check e-tag.. if not equivalent to what we know, reload?

        switch (toWrite.Version) {
            case -3: // no stream
                if (_idStreams.TryGetValue(toWrite.Id, out var noStream)) return WriteResult.Failed(_position, noStream.Last().Revision, new StreamExistsException());
                break;
            case -2: // any stream
                break;
            case -1: // empty stream
                if (!_idStreams.TryGetValue(toWrite.Id, out var emptyStream)) return WriteResult.Failed(-1, ExpectedVersion.NoStream, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, ExpectedVersion.NoStream));
                if (emptyStream.Length != 0) return WriteResult.Failed(_position, -1, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, emptyStream.Length));
                break;
            default:
                if (!_idStreams.TryGetValue(toWrite.Id, out var possibleStream)) return WriteResult.Failed(_position, ExpectedVersion.NoStream, new WrongExpectedVersionException(toWrite.Version, ExpectedVersion.NoStream));
                if (possibleStream != null && possibleStream.Length != toWrite.Version) {
                    var existingEventIds = possibleStream.Select(ps => ps.Event.EventId).ToArray();

                    // if all events are appended, considered as a double request and post-back ok.
                    if (toWrite.Events.All(e => existingEventIds.Contains(e.EventId))) {
                        return WriteResult.Ok(_position, possibleStream.Last().Revision);
                    }

                    // if all events were not appended -- or -- only some were appended, then throw a wrong expected version.
                    if (toWrite.Events.All(e => !existingEventIds.Contains(e.EventId)) || toWrite.Events.Any(e => existingEventIds.Contains(e.EventId))) {
                        return WriteResult.Failed(_position, possibleStream.Last().Revision, new WrongExpectedVersionException(toWrite.Version, possibleStream.Length));
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
        return WriteResult.Ok(_position, lastRevision);
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

    private record Work(StreamId Id, ExpectedVersion Version, EventData[] Events);
}
