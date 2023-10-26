using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args);
host.AddLvStreamStore()
    .UseEmbeddedClient()
    .UseMemoryStorage()
    .UseJsonSerialization();

host.Build().Run();