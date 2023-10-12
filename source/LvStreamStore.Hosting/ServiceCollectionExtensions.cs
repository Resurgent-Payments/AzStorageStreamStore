namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddLvStreamStore<TImplementation>(this IServiceCollection services) where TImplementation : EventStream {
        services.AddSingleton<InMemoryBus>();
        services.AddSingleton<EventStream, TImplementation>();

        return services;
    }
}