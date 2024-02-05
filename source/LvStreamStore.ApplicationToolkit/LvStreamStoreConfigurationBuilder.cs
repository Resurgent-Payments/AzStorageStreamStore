namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;
using LvStreamStore.ApplicationToolkit;

using Microsoft.Extensions.Hosting;

public static class LvStreamStoreConfigurationBuilderExtensions {

    public static ApplicationToolkitConfigurationBuilder AddApplicationToolkit(this LvStreamStoreConfigurationBuilder builder) {
        builder.Builder.ConfigureServices((ctx, services) => {
            //services.AddSingleton<IDispatcher, Dispatcher>();
            //services.AddSingleton<ISubscriber>((provider) => provider.GetRequiredService<IDispatcher>());
            //services.AddSingleton<IPublisher>((provider) => provider.GetRequiredService<IDispatcher>());
            //services.AddSingleton<ICommandPublisher>((provider) => provider.GetRequiredService<IDispatcher>());

            services.AddSingleton<IStreamStoreRepository, StreamStoreRepository>();
            //services.AddHostedService<AutoStartServicesHostedService>();
        });
        return new ApplicationToolkitConfigurationBuilder(builder);
    }

    public static IHost UseApplicationToolkit(this IHost host) {
        _ = host.Services.GetServices<ReadModelBase>();
        _ = host.Services.GetServices<TransientSubscriber>();

        return host;
    }
}