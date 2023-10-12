namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;

using Microsoft.Extensions.Hosting;

public static class HostBuilderExtensions {
    public static IHostBuilder UseLvStreamStore<TImplementation>(IHostBuilder host) where TImplementation : EventStream {
        host.ConfigureServices((ctx, svc) => {
            svc.AddLvStreamStore<TImplementation>();
        });

        // todo: do we need a pump to have it catch-up?

        return host;
    }
}