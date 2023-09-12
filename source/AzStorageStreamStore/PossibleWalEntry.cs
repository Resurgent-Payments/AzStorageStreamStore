namespace AzStorageStreamStore;
public record PossibleWalEntry(TaskCompletionSource<WriteResult> OnceCompleted, StreamId Id, ExpectedVersion Version, EventData[] Events);
