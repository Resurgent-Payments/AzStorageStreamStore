namespace LvStreamStore;

using Microsoft.Extensions.Hosting;

public class LvStreamStoreConfigurationBuilder {
    public IHostBuilder Builder { get; }

    public LvStreamStoreConfigurationBuilder(IHostBuilder builder) {
        Builder = builder;
    }
}
