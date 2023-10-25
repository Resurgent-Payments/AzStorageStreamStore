namespace LvStreamStore;

public record WriteToStreamArgs(TaskCompletionSource<WriteResult> OnceCompleted, StreamId Id, ExpectedVersion Version, IEnumerable<EventData> Events);
