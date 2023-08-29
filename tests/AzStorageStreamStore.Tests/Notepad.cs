namespace AzStorageStreamStore.Tests;

using System.Text.Json;

using Xunit;

public class Notepad {
    [Fact]
    public async Task A_stream_can_be_written_and_read() {
        List<long> intervals = new();

        long startTime = DateTime.UtcNow.Ticks;

        var tempFile = Path.GetTempFileName();
        var eventsToWrite = Enumerable.Range(1, 50000)
            .Select(x => new SomeRecord(Guid.NewGuid(), x, $"Text {x}"))
            .ToArray();
        using (var file = File.OpenWrite(tempFile)) {
            await JsonSerializer.SerializeAsync(file, eventsToWrite);
            await file.FlushAsync();
        }
        long endTime = DateTime.UtcNow.Ticks;

        intervals.Add(endTime - startTime);

        startTime = DateTime.UtcNow.Ticks;

        SomeRecord[] someReadRecords;
        using (var file = File.OpenRead(tempFile)) {
            var doc = await JsonDocument.ParseAsync(file);
            someReadRecords = doc.RootElement.EnumerateArray().Skip(40000).Select(x => x.Deserialize<SomeRecord>()).ToArray();

        }

        endTime = DateTime.UtcNow.Ticks;
        intervals.Add(endTime - startTime);

        Assert.Equal(10000, someReadRecords.Length);

        var something = intervals.Select(i => $"{TimeSpan.FromTicks(i).TotalMilliseconds}ms.").ToArray();

        var x = 0;
    }

    public record SomeRecord(Guid Id, int Count, string SomeText);
}