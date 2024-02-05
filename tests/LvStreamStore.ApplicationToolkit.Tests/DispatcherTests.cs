namespace LvStreamStore.ApplicationToolkit.Tests {
    using LvStreamStore.Messaging;

    using Xunit;

    public class DispatcherTests : IAsyncDisposable {
        private readonly AsyncDispatcher _dispatcher;

        public DispatcherTests() {
            _dispatcher = new AsyncDispatcher();
        }

        [Fact]
        public async Task PublishesEvents() {
            var mre = new ManualResetEventSlim(false);
            await _dispatcher.HandleAsync(
                AsyncDispatcher.Register(new AdHocReceiver<TestMessages.TestEvent1>(_ => { mre.Set(); return Task.CompletedTask; }))
            );
            await _dispatcher.BroadcastAsync(new TestMessages.TestEvent1());
            mre.Wait(100);
            Assert.True(mre.IsSet);
        }

        [Fact]
        public async Task EventSubscriptionsAreSpecificToEventTypes() {
            var mre1 = new ManualResetEventSlim(false);
            var mre2 = new ManualResetEventSlim(false);
            await _dispatcher.HandleAsync(
                AsyncDispatcher.Register(new AdHocReceiver<TestMessages.TestEvent1>(_ => { mre1.Set(); return Task.CompletedTask; }))
            );
            await _dispatcher.HandleAsync(
                AsyncDispatcher.Register(new AdHocReceiver<TestMessages.TestEvent2>(_ => { mre2.Set(); return Task.CompletedTask; }))
            );


            await _dispatcher.BroadcastAsync(new TestMessages.TestEvent1());

            mre1.Wait(100);
            mre2.Wait(100);

            Assert.True(mre1.IsSet);
            Assert.False(mre2.IsSet);
        }

        [Fact]
        public async Task PublishesCommands() {
            var mre1 = new ManualResetEventSlim(false);

            await _dispatcher.HandleAsync(
                AsyncDispatcher.Register(new AdHocReceiver<TestMessages.TestEvent1>(_ => { mre1.Set(); return Task.CompletedTask; }))
            );

            await _dispatcher.BroadcastAsync(new TestMessages.TestEvent1());

            Assert.True(mre1.IsSet);
        }

        [Fact]
        public async Task PublishedCommandsCanTimeoutAfterDuration() {
            var mre1 = new ManualResetEventSlim(false);
            await _dispatcher.HandleAsync(AsyncDispatcher.Register(new AdHocHandler<TestMessages.TestCommand1>(async (cmd) => {
                await Task.Delay(TimeSpan.FromSeconds(5));
                mre1.Set();
            })));

            _ = await Assert.ThrowsAsync<TimeoutException>(async () => await _dispatcher.SendAsync(new TestMessages.TestCommand1(), TimeSpan.FromSeconds(1)));

            Assert.False(mre1.IsSet);
        }

        [Fact]
        public async Task ThowsCommandHandlerNotRegisteredExceptionWhenHandlerNotRegistered() {
            await Assert.ThrowsAsync<MissingHandlerException>(async () => 
                await _dispatcher.SendAsync(new TestMessages.NotRegisteredCommand()));
        }

        [Fact]
        public async Task ShouldPublishEventWithoutHandlersSuccessfully() {
            await _dispatcher.BroadcastAsync(new TestMessages.NotRegisteredEvent());
        }

        public ValueTask DisposeAsync() {
            return ValueTask.CompletedTask;
        }
    }
}
