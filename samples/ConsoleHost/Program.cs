using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args);
host.ConfigureServices((ctx, services) => {
    services.AddLvStreamStore();
});

host.Build().Run();