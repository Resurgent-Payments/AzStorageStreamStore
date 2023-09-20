namespace AzStorageStreamStore;

internal class PersistenceUtils {
    private readonly IPersister _persister;

    public PersistenceUtils(IPersister persister) {
        _persister = persister;
    }

    public async ValueTask<bool> PassesStreamValidationAsync(TaskCompletionSource<WriteResult> onceCompleted, StreamId streamId, ExpectedVersion expected, EventData[] events) {

        try {
            switch (expected) {
                case -3: // no stream
                    if (!await _persister.ReadStreamAsync(StreamKey.All).AllAsync(e => e.StreamId != streamId)) {
                        onceCompleted.SetResult(WriteResult.Failed(_persister.Position, -1, new StreamExistsException()));
                        return false;
                    }
                    break;
                case -2: // any stream
                    break;
                case -1: // empty stream
                    if (await _persister.ReadStreamAsync(StreamKey.All).AllAsync(s => s.StreamId != streamId)) {
                        var revision = _persister.ReadStreamAsync(StreamKey.All).OfType<RecordedEvent>().MaxAsync(e => e.Revision);
                        onceCompleted.SetResult(WriteResult.Failed(_persister.Position, -1, new WrongExpectedVersionException(ExpectedVersion.EmptyStream, -1)));
                        return false;
                    } else {
                        // check for duplicates here.
                        var nonEmptyStreamEvents = await _persister.ReadStreamAsync(StreamKey.All).OfType<RecordedEvent>().Where(s => s.StreamId == streamId).ToListAsync();

                        if (nonEmptyStreamEvents.Any()) {
                            // if all events are appended, considered as a double request and post-back ok.
                            if (!nonEmptyStreamEvents.All(e => events.All(i => e.EventId != i.EventId))) {
                                onceCompleted.SetResult(WriteResult.Ok(_persister.Position, nonEmptyStreamEvents.Max(x => x.Revision)));
                                return false;
                            }
                        }
                    }
                    break;
                default:
                    var filtered = await _persister.ReadStreamAsync(StreamKey.All).OfType<RecordedEvent>().Where(e => e.StreamId == streamId).ToListAsync();

                    if (!filtered.Any()) {
                        onceCompleted.SetResult(WriteResult.Failed(_persister.Position, -1, new WrongExpectedVersionException(expected, ExpectedVersion.NoStream)));
                        return false;
                    }

                    if (filtered.Count() != expected) {
                        // if all events are appended, considered as a double request and post-back ok.
                        if (events.All(e => filtered.All(i => i.EventId != e.EventId))) {

                            onceCompleted.SetResult(WriteResult.Ok(_persister.Position, filtered.Max(x => x.Revision)));
                            return false;
                        }

                        // if all events were not appended
                        // -- or --
                        // only some were appended, then throw a wrong expected version.
                        if (events.Select(e => filtered.All(s => s.EventId != e.EventId)).Any()) {
                            onceCompleted.SetResult(WriteResult.Failed(_persister.Position,
                                filtered.Max(x => x.Revision),
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
}