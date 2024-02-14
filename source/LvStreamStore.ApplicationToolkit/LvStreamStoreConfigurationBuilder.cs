namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;
using LvStreamStore.ApplicationToolkit;
using LvStreamStore.Messaging;

using Microsoft.Extensions.Hosting;

public static class LvStreamStoreConfigurationBuilderExtensions {

    public static ApplicationToolkitConfigurationBuilder AddApplicationToolkit(this LvStreamStoreConfigurationBuilder builder) {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<AsyncDispatcher>();
            services.AddSingleton<IStreamStoreRepository, StreamStoreRepository>();
        });
        return new ApplicationToolkitConfigurationBuilder(builder);
    }

    public static IHost UseApplicationToolkit(this IHost host) {
        _ = host.Services.GetServices<ReadModelBase>();
        _ = host.Services.GetServices<TransientSubscriber>();

        return host;
    }
}