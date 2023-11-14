namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;

using LvStreamStore.ApplicationToolkit;
using LvStreamStore.ApplicationToolkit.WebHooks;

public static class LvStreamStoreAspNetCoreConfigurationBuilderExtensions {

    public static LvStreamStoreConfigurationBuilder UseWebHooks(this LvStreamStoreConfigurationBuilder builder, Action<WebHookOptions> configure) {
        var options = new WebHookOptions();

        configure(options);

        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<IAutoStartService, WebHookSubscriptionService>();

            services.AddSingleton(sp => {
                var rm = new WebHookRm(sp.GetRequiredService<ISubscriber>(), sp.GetRequiredService<IStreamStoreRepository>());

                var messageTypes = options.DiscoveryAssemblies.SelectMany(x =>
                    x.GetTypes()
                        .Select(t => new { Type = t, HasAttribute = t.GetCustomAttribute<WebHookMessageAttribute>() != null })
                        .Where(x => x.HasAttribute)
                );
                foreach (var messageType in messageTypes) {
                    rm.RegisterMessage(messageType.Type);
                }

                return rm;
            });
            services.AddSingleton<IAutoStartService>(sp => sp.GetRequiredService<WebHookRm>());

            var mvcBuilder = services.AddMvcCore();

            mvcBuilder.AddApplicationPart(typeof(WebHookDiscoveryController).Assembly);
            foreach (var asm in options.DiscoveryAssemblies) {
                mvcBuilder.AddApplicationPart(asm);
            }
        });
        return builder;
    }

}

public class WebHookOptions {
    internal List<Assembly> DiscoveryAssemblies { get; } = new();

    public void RegisterAssembly(Assembly assembly) {
        DiscoveryAssemblies.Add(assembly);
    }

    public void RegisterAssemblyFromType<T>() {
        DiscoveryAssemblies.Add(typeof(T).Assembly);
    }
}