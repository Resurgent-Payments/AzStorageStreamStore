namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddLvStreamStore<TImplementation>(this IServiceCollection services, Action<LvStreamStoreConfiguration> configure) where TImplementation : EventStream {
        var configuration = new LvStreamStoreConfiguration();

        configure(configuration);

        services.AddSingleton<InMemoryBus>();
        services.AddSingleton<EventStream, TImplementation>();

        if (configuration.RunBackgroundService) {
            //services.AddHostedService<>();
        }

        return services;
    }

    public class LvStreamStoreConfiguration {
        public bool RunBackgroundService { get; set; } = false;
    }
}