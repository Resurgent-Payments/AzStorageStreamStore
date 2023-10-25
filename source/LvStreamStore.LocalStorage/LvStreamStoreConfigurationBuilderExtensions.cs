namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;

public static class LvStreamStoreConfigurationBuilderExtensions {
    public static LvStreamStoreConfigurationBuilder UseLocalStorage(this LvStreamStoreConfigurationBuilder builder, Action<LocalStorageEventStreamOptions> options) {
        var registered = builder.Services.FirstOrDefault(x => x.ServiceType == typeof(EventStream));

        if (registered != null) {
            throw new InvalidOperationException("Event Stream has already been registered.");
        }

        builder.Services.Configure(options);
        builder.Services.AddSingleton<EventStream, LocalStorageEventStream>();

        return builder;
    }
    public static LvStreamStoreConfigurationBuilder UseLocalStorage(this LvStreamStoreConfigurationBuilder builder) {
        builder.UseLocalStorage(opts => { });
        return builder;
    }
}