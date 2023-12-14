namespace LvStreamStore.ApplicationToolkit.Tests {
    using Microsoft.Extensions.Logging;

    using Xunit;

    public class DispatcherTests : IAsyncDisposable {
        private readonly IDispatcher _dispatcher;
        private readonly ILoggerFactory _loggerFactory;
        private List<IDisposable> _disposables = new();

        public DispatcherTests() {
            _loggerFactory = LoggerFactory.Create(config => {
                config.AddDebug();
            });
            _dispatcher = new Dispatcher(_loggerFactory);
        }

        [Fact]
        public async Task PublishesEvents() {
            var mre = new ManualResetEventSlim(false);
            _disposables.AddRange(new[] {
                _dispatcher.Subscribe(new AsyncAdHocHandler<TestMessages.TestEvent1>((_) => { mre.Set(); return ValueTask.CompletedTask; }))
            });
            await _dispatcher.PublishAsync(new TestMessages.TestEvent1());
            mre.Wait(100);
            Assert.True(mre.IsSet);
        }

        [Fact]
        public async Task EventSubscriptionsAreSpecificToEventTypes() {
            var mre1 = new ManualResetEventSlim(false);
            var mre2 = new ManualResetEventSlim(false);

            _disposables.AddRange(new[] {
                _dispatcher.Subscribe(new AsyncAdHocHandler<TestMessages.TestEvent1>((_) => { mre1.Set(); return ValueTask.CompletedTask; })),
                _dispatcher.Subscribe(new AsyncAdHocHandler<TestMessages.TestEvent2>((_) => { mre2.Set(); return ValueTask.CompletedTask; }))
            });

            await _dispatcher.PublishAsync(new TestMessages.TestEvent1());

            mre1.Wait(100);
            mre2.Wait(100);

            Assert.True(mre1.IsSet);
            Assert.False(mre2.IsSet);
        }

        [Fact]
        public async Task PublishesCommands() {
            var mre1 = new ManualResetEventSlim(false);

            _disposables.AddRange(new[] {
                _dispatcher.Subscribe(new AsyncAdHocCommandHandler<TestMessages.TestCommand1>((cmd) => {mre1.Set(); return ValueTask.FromResult(cmd.Complete()); }))
            });

            var result = await _dispatcher.SendAsync(new TestMessages.TestCommand1());

            Assert.IsType<CommandCompleted>(result);
            Assert.True(mre1.IsSet);
        }

        [Fact]
        public async Task PublishedCommandsCanTimeoutAfterDuration() {
            var mre1 = new ManualResetEventSlim(false);

            _disposables.AddRange(new[] {
                _dispatcher.Subscribe(new AsyncAdHocCommandHandler<TestMessages.TestCommand1>( async (cmd) => {
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    mre1.Set();
                    return cmd.Complete();
                }))
            });

            _ = await Assert.ThrowsAsync<TimeoutException>(async () => await _dispatcher.SendAsync(new TestMessages.TestCommand1(), TimeSpan.FromSeconds(1)));

            Assert.False(mre1.IsSet);
        }

        [Fact]
        public async Task ThowsCommandHandlerNotRegisteredExceptionWhenHandlerNotRegistered() {
            //note: not sure I like this.  need to see how this can be handled differently.
            var exc = await Assert.ThrowsAsync<AggregateException>(async () => await _dispatcher.SendAsync(new TestMessages.NotRegisteredCommand()));
            Assert.IsType<MissingCommandHandlerException>(exc.InnerException);
        }

        [Fact]
        public async Task ShouldPublishEventWithoutHandlersSuccessfully() {
            await _dispatcher.PublishAsync(new TestMessages.NotRegisteredEvent());
        }

        public ValueTask DisposeAsync() {
            _dispatcher?.Dispose();
            _loggerFactory?.Dispose();

            foreach (var disposable in _disposables ?? Enumerable.Empty<IDisposable>()) {
                disposable?.Dispose();
            }
            _disposables?.Clear();

            return ValueTask.CompletedTask;
        }
    }
}
