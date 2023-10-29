namespace LvStreamStore;
public record BusTelemetry(long NumberOfEventsProcessed, long AverageExecutionTime, Guid? MsgId = null) : StreamEvent(MsgId);