namespace LvStreamStore.Tests;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public class EventBusTests {
    [Fact]
    public async Task Can_publish_an_event() {
        var tcs = new TaskCompletionSource();
        var bus = new EventBus(NullLogger.Instance);
        var @event = new StreamEvent();
        StreamEvent? received = null;
        bus.Subscribe(new AdHocHandler<StreamEvent>(e => {
            received = e;
            tcs.SetResult();
            return ValueTask.CompletedTask;
        }));
        await bus.PublishAsync(@event);

        await tcs.Task.WithTimeout(TimeSpan.FromSeconds(2));
        Assert.Same(@event, received);
    }

    [Fact]
    public async Task Can_handle_multiple_subscriptions() {
        var tcs = new TaskCompletionSource();
        var bus = new EventBus(NullLogger.Instance);
        var @event = new StreamEvent();
        StreamEvent? received1 = null;
        StreamEvent? received2 = null;
        bus.Subscribe(new AdHocHandler<StreamEvent>(e => {
            received1 = e;
            tcs.SetResult();
            return ValueTask.CompletedTask;
        }));
        bus.Subscribe(new AdHocHandler<StreamEvent>(e => {
            received2 = e;
            tcs.SetResult();
            return ValueTask.CompletedTask;
        }));
        await bus.PublishAsync(@event);

        await tcs.Task.WithTimeout(TimeSpan.FromSeconds(2));
        Assert.Same(@event, received1);
        Assert.Same(@event, received2);
    }
}