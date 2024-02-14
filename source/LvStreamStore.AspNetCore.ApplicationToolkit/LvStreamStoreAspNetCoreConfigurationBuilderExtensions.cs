namespace Microsoft.Extensions.DependencyInjection;

using System.Reflection;

using LvStreamStore;
using LvStreamStore.ApplicationToolkit;
using LvStreamStore.ApplicationToolkit.WebHooks;
using LvStreamStore.Messaging;

public static class LvStreamStoreAspNetCoreConfigurationBuilderExtensions {

    public static ApplicationToolkitConfigurationBuilder UseWebHooks(this ApplicationToolkitConfigurationBuilder builder, Action<WebHookOptions> configure) {
        var options = new WebHookOptions();

        configure(options);

        builder.Builder.ConfigureServices((ctx, services) => {
            services.AddSingleton<TransientSubscriber, SubscriptionService>();
            services.AddSingleton<TransientSubscriber, SubscriptionCallbackService>();

            services.AddSingleton(sp => {
                var rm = new WebHookRm(sp.GetRequiredService<AsyncDispatcher>(), sp.GetRequiredService<IStreamStoreRepository>());

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
            services.AddSingleton<ReadModelBase>(sp => sp.GetRequiredService<WebHookRm>());

            var mvcBuilder = services.AddMvcCore();

            mvcBuilder.AddApplicationPart(typeof(DiscoveryController).Assembly);
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