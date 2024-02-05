namespace LvStreamStore;

using LvStreamStore.Messaging;

public record BusTelemetry(long NumberOfEventsProcessed, long AverageExecutionTime) : Message;