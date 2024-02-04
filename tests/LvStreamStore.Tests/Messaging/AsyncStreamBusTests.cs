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
    class TestException : Exception { }
}
