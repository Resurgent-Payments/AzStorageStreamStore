using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args);
host.ConfigureServices(services => {
    services.AddLvStreamStore()
        .UseMemoryStorage()
        .UseJsonSerialization();
});

host.Build().Run();