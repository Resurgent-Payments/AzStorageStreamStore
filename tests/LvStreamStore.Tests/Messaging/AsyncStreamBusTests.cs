namespace LvStreamStore.Tests.Messaging;

using LvStreamStore.Messaging;

using Xunit;

public class AsyncStreamBusTests {
    [Fact]
    public async Task A_bus_can_use_a_handler() {
        var bus = new AsyncDispatcher();
        var tcs = new TaskCompletionSource();
        var handler = new TestHandler(tcs);
        await bus.HandleAsync(AsyncDispatcher.Register(handler));

        await bus.HandleAsync(new TestMessage()).WithTimeout(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task A_sent_message_can_timeout() {
        var bus = new AsyncDispatcher();
        var tcs = new TaskCompletionSource();
        var handler = new TestHandler(tcs);
        await bus.HandleAsync(AsyncDispatcher.Register(handler));

        await Assert.ThrowsAsync<TimeoutException>(() =>
            bus.HandleAsync(new TestMessage()).WithTimeout(TimeSpan.FromMilliseconds(50)));
    }

    [Fact]
    public async Task Bubbles_thrown_exceptions_to_caller() {
        var bus = new AsyncDispatcher();
        var tcs = new TaskCompletionSource();
        var handler = new FailingTestHandler();
        await bus.HandleAsync(AsyncDispatcher.Register(handler));

        await Assert.ThrowsAsync<TestException>(() =>
            bus.HandleAsync(new TestMessage()));
    }

    [Fact]
    public async Task Can_subscribe_and_broadcast() {
        var bus = new AsyncDispatcher();
        TaskCompletionSource tcs = new();
        var receiver = new TestReceiver(tcs);
        await bus.HandleAsync(AsyncDispatcher.Register(receiver));
        await bus.BroadcastAsync(new TestMessage()).WithTimeout(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task Can_broadcast_multiple_times() {
        var bus = new AsyncDispatcher();
        TaskCompletionSource tcs1 = new();
        TaskCompletionSource tcs2 = new();
        var receiver1 = new TestReceiver(tcs1);
        var receiver2 = new TestReceiver(tcs2);
        await bus.HandleAsync(AsyncDispatcher.Register(receiver1));
        await bus.HandleAsync(AsyncDispatcher.Register(receiver2));
        await bus.BroadcastAsync(new TestMessage());
        await Task.WhenAll([tcs1.Task, tcs2.Task]).WithTimeout(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public async Task Can_dispose_a_receiver() {
        var bus = new AsyncDispatcher();

        TaskCompletionSource tcs1 = new();
        var receiver1 = new TestReceiver(tcs1);
        var registration1 = AsyncDispatcher.Register(receiver1);
        await bus.HandleAsync(registration1);

        TaskCompletionSource tcs2 = new();
        var receiver2 = new TestReceiver(tcs2);
        var registration2 = AsyncDispatcher.Register(receiver2);
        await bus.HandleAsync(registration2);
        if (registration2 is IDisposable disposer) { disposer.Dispose(); }

        await bus.BroadcastAsync(new TestMessage());
        await tcs1.Task.WithTimeout(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAsync<TimeoutException>(() => 
            tcs2.Task.WithTimeout(TimeSpan.FromMilliseconds(300)));
    }

    [Fact]
    public async Task A_message_can_be_sent() {
        var bus = new AsyncDispatcher();
        var tcs = new TaskCompletionSource();
        var handler = new TestHandler(tcs);
        await bus.HandleAsync(AsyncDispatcher.Register(handler));

        await bus.SendAsync(new TestMessage()).WithTimeout(TimeSpan.FromMilliseconds(200));
    }

    record TestMessage : Message;
    class TestHandler : IHandleAsync<TestMessage> {
        TaskCompletionSource _tcs;
        public TestHandler(TaskCompletionSource tcs) {
            _tcs = tcs;
        }
        public async Task HandleAsync(TestMessage message) {
            await Task.Delay(100);
            _tcs.TrySetResult();
        }
    }
    class FailingTestHandler : IHandleAsync<TestMessage> {
        public Task HandleAsync(TestMessage _)
            => Task.FromException(new TestException());
    }

    class TestReceiver : IReceiver<TestMessage> {
        TaskCompletionSource _tcs = new();
        public TestReceiver(TaskCompletionSource tcs) {
            _tcs = tcs;
        }

#pragma warning disable CS1998 // being lazy.
        public async Task Receive(TestMessage _)
#pragma warning restore CS1998 // being lazy.
            => _tcs.SetResult();
    }
    class TestException : Exception { }
}
