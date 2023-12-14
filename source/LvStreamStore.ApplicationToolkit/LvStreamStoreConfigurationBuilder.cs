namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;
using LvStreamStore.ApplicationToolkit;

public static class LvStreamStoreConfigurationBuilderExtensions {

    public static LvStreamStoreConfigurationBuilder UseApplicationToolkit(this LvStreamStoreConfigurationBuilder builder) {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<IDispatcher, Dispatcher>();
            services.AddSingleton<ISubscriber>((provider) => provider.GetRequiredService<IDispatcher>());
            services.AddSingleton<IPublisher>((provider) => provider.GetRequiredService<IDispatcher>());
            services.AddSingleton<ICommandPublisher>((provider) => provider.GetRequiredService<IDispatcher>());

            services.AddSingleton<IStreamStoreRepository, StreamStoreRepository>();
            services.AddHostedService<AutoStartServicesHostedService>();
        });
        return builder;
    }

    public static LvStreamStoreConfigurationBuilder RegisterAutoStartService<TService>(this LvStreamStoreConfigurationBuilder builder) where TService : IAutoStartService {
        builder.RegisterAutoStartService(typeof(TService));

        return builder;
    }

    public static LvStreamStoreConfigurationBuilder RegisterAutoStartService(this LvStreamStoreConfigurationBuilder builder, Type serviceType) {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton(typeof(IAutoStartService), serviceType);
        });

        return builder;
    }
}