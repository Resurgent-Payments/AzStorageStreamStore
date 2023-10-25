namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;

public static class LvStreamStoreConfigurationBuilderExtensions {
    public static LvStreamStoreConfigurationBuilder UseLocalStorage(this LvStreamStoreConfigurationBuilder builder, Action<LocalStorageEventStreamOptions> options) {
        builder.Builder.ConfigureServices((ctx, services) => {
            var registered = services.FirstOrDefault(x => x.ServiceType == typeof(EventStream));

            if (registered != null) {
                throw new InvalidOperationException("Event Stream has already been registered.");
            }

            services.Configure(options);
            services.AddSingleton<EventStream, LocalStorageEventStream>();
        });

        return builder;
    }
    public static LvStreamStoreConfigurationBuilder UseLocalStorage(this LvStreamStoreConfigurationBuilder builder) {
        builder.UseLocalStorage(opts => { });
        return builder;
    }
}