using LvStreamStore;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationBuilderExtensions {
    public static IApplicationBuilder UseLvStreamStore(this IApplicationBuilder builder) {
        // todo: wire me up scotty!

        builder.Use(async (ctx, app) => {
            // would this be super-slow?
            var bus = ctx.RequestServices.GetRequiredService<InMemoryBus>();
            await bus.PublishAsync(new UpdateSubscription());
        });

        var bus = builder.ApplicationServices.GetRequiredService<InMemoryBus>();
        AsyncHelper.RunSync(() => bus.PublishAsync(new UpdateSubscription()));

        return builder;
    }
}