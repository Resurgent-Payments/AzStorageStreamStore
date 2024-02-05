namespace LvStreamStore.Tests;

using LvStreamStore.Messaging;

using Xunit;

public class EventBusTests {
    [Fact]
    public async Task Can_publish_an_event() {
        var tcs = new TaskCompletionSource();
        var bus = new AsyncDispatcher();
        var @event = new StreamEvent();
        StreamEvent? received = null;
        await bus.HandleAsync(AsyncDispatcher.Register(new AdHocReceiver<StreamEvent>(e => {
            received = e;
            tcs.SetResult();
            return Task.CompletedTask;
        })));
        await bus.BroadcastAsync(@event);

        await tcs.Task.WithTimeout(TimeSpan.FromSeconds(2));
        Assert.Same(@event, received);
    }

    [Fact]
    public async Task Can_handle_multiple_subscriptions() {
        var tcs = new TaskCompletionSource();
        var bus = new AsyncDispatcher();
        var @event = new StreamEvent();
        StreamEvent? received1 = null;
        StreamEvent? received2 = null;
        await bus.HandleAsync(AsyncDispatcher.Register(new AdHocReceiver<StreamEvent>(e => {
            received1 = e;
            tcs.SetResult();
            return Task.CompletedTask;
        })));
        await bus.HandleAsync(AsyncDispatcher.Register(new AdHocReceiver<StreamEvent>(e => {
            received2 = e;
            tcs.SetResult();
            return Task.CompletedTask;
        })));
        await bus.BroadcastAsync(@event);

        await tcs.Task.WithTimeout(TimeSpan.FromSeconds(2));
        Assert.Same(@event, received1);
        Assert.Same(@event, received2);
    }
}