namespace LvStreamStore;

using System;
using System.Linq;

using LvStreamStore.Serialization;
using LvStreamStore.Serialization.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

public static class LvStreamStoreConfigurationBuilderExtensions {
    public static LvStreamStoreConfigurationBuilder AddLvStreamStore(this IHostBuilder builder) {
        return new LvStreamStoreConfigurationBuilder(builder);
    }

    public static LvStreamStoreConfigurationBuilder UseEmbeddedClient(this LvStreamStoreConfigurationBuilder builder) {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<IEventStreamClient, EmbeddedEventStreamClient>();
        });
        return builder;
    }

    public static LvStreamStoreConfigurationBuilder UseMemoryStorage(this LvStreamStoreConfigurationBuilder builder, Action<MemoryEventStreamOptions> options) {
        builder.Builder.ConfigureServices((ctx, services) => {
            var registered = services.FirstOrDefault(x => x.ServiceType == typeof(EventStream));

            if (registered != null) {
                throw new InvalidOperationException("Event Stream has already been registered.");
            }

            services.Configure(options);
            services.TryAddSingleton<EventStream, MemoryEventStream>();
        });


        return builder;
    }

    public static LvStreamStoreConfigurationBuilder UseMemoryStorage(this LvStreamStoreConfigurationBuilder builder) {
        builder.UseMemoryStorage(_ => { });
        return builder;
    }

    public static LvStreamStoreConfigurationBuilder UseJsonSerialization(this LvStreamStoreConfigurationBuilder builder) {
        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddOptions<JsonSerializationOptions>();
            services.AddTransient<IEventSerializer, JsonEventSerializer>();
        });

        return builder;
    }
}
