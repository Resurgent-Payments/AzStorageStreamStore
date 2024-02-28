namespace LvStreamStore.Test;

using LvStreamStore.Messaging;

using Xunit;

public interface IClientTestFixture : IAsyncLifetime {
    EventStream Stream { get; }
    IEventStreamClient Client { get; }
    AsyncDispatcher Dispatcher { get; }
}
