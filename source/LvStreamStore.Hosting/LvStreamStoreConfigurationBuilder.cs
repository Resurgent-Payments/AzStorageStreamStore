namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;
using LvStreamStore.Serialization;
using LvStreamStore.Serialization.Json;

using Microsoft.Extensions.DependencyInjection.Extensions;

public class LvStreamStoreConfigurationBuilder {
    public IServiceCollection Services { get; }

    public LvStreamStoreConfigurationBuilder(IServiceCollection services) {
        Services = services;
    }
}

public static class LvStreamStoreConfigurationBuilderExtensions {
    public static LvStreamStoreConfigurationBuilder AddLvStreamStore(this IServiceCollection services) {
        return new LvStreamStoreConfigurationBuilder(services);
    }

    public static LvStreamStoreConfigurationBuilder UseMemoryStorage(this LvStreamStoreConfigurationBuilder builder, Action<MemoryEventStreamOptions> options) {
        var registered = builder.Services.FirstOrDefault(x => x.ServiceType == typeof(EventStream));

        if (registered != null) {
            throw new InvalidOperationException("Event Stream has already been registered.");
        }

        builder.Services.Configure(options);
        builder.Services.TryAddSingleton<EventStream, MemoryEventStream>();

        return builder;
    }

    public static LvStreamStoreConfigurationBuilder UseMemoryStorage(this LvStreamStoreConfigurationBuilder builder) {
        builder.UseMemoryStorage(_ => { });
        return builder;
    }

    public static LvStreamStoreConfigurationBuilder UseJsonSerialization(this LvStreamStoreConfigurationBuilder builder) {
        builder.Services.AddOptions<JsonSerializationOptions>();
        builder.Services.AddTransient<IEventSerializer, JsonEventSerializer>();

        return builder;
    }
}