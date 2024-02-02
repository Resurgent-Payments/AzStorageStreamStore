namespace Microsoft.Extensions.DependencyInjection;

using LvStreamStore;
using LvStreamStore.ApplicationToolkit;
using LvStreamStore.Authentication;

public static class LvStreamStoreConfigurationBuilderExtensions {
    public static LvStreamStoreConfigurationBuilder UseAuthentication(this LvStreamStoreConfigurationBuilder builder) {
        builder.UseAuthentication(_ => { });
        return builder;
    }

    public static LvStreamStoreConfigurationBuilder UseAuthentication(this LvStreamStoreConfigurationBuilder builder, Action<PasswordHasherOptions> onConfiguration) {

        builder.Builder.ConfigureServices((ctx, services) => {

            services.AddSingleton<ReadModelBase, ApplicationTenantService>();
            services.AddSingleton<ReadModelBase, UserService>();
            services.AddTransient<ITenantService, SingleTenantService>();

            services.Configure(onConfiguration);
        });

        return builder;
    }
}
