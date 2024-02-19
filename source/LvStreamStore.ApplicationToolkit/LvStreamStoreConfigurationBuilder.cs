namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;
using LvStreamStore.ApplicationToolkit;
using LvStreamStore.Messaging;

using Microsoft.Extensions.Hosting;

public static class LvStreamStoreConfigurationBuilderExtensions {

    public static ApplicationToolkitConfigurationBuilder UseApplicationToolkit(this LvStreamStoreConfigurationBuilder builder) {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<AsyncDispatcher>();
            services.AddSingleton<IStreamStoreRepository, StreamStoreRepository>();
        });
        return new ApplicationToolkitConfigurationBuilder(builder);
    }

    /// <summary>
    /// After the build of the app, call `app.UseApplicationToolkit()` to setup the transient subscribers and readmodels for use.
    /// </summary>
    /// <param name="host"></param>
    /// <returns></returns>
    public static IHost InitializeApplicationToolkit(this IHost host) {
        _ = host.Services.GetServices<ReadModelBase>();
        _ = host.Services.GetServices<TransientSubscriber>();

        return host;
    }

    public static ApplicationToolkitConfigurationBuilder RegisterSubscriber<TSubscriber>(this ApplicationToolkitConfigurationBuilder builder) where TSubscriber : TransientSubscriber {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<TSubscriber>();
            services.AddSingleton<TransientSubscriber>((sp) => sp.GetRequiredService<TSubscriber>());
        });
        return builder;
    }

    public static ApplicationToolkitConfigurationBuilder RegisterModel<TModel>(this ApplicationToolkitConfigurationBuilder builder) where TModel : ReadModelBase {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<TModel>();
            services.AddSingleton<ReadModelBase>((sp) => sp.GetRequiredService<TModel>());
        });
        return builder;
    }

}