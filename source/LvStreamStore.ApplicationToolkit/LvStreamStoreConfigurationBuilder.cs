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

    public static ApplicationToolkitConfigurationBuilder RegisterSubscriber<TSubscriber>(this ApplicationToolkitConfigurationBuilder builder) where TSubscriber : TransientSubscriber {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<TransientSubscriber, TSubscriber>();
        });
        return builder;
    }

    public static ApplicationToolkitConfigurationBuilder RegisterModel<TModel>(this ApplicationToolkitConfigurationBuilder builder) where TModel : ReadModelBase {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<ReadModelBase, TModel>();
        });
        return builder;
    }

}