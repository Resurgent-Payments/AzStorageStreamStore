namespace LvStreamStore.Test;

using LvStreamStore.Messaging;

public interface IClientTestFixture : IDisposable {
    EventStream Stream { get; }
    IEventStreamClient Client { get; }
    AsyncDispatcher Dispatcher { get; }
}
