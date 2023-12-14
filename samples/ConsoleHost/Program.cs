using LvStreamStore;

using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args);
host.AddLvStreamStore()
    .UseEmbeddedClient()
    .UseMemoryStorage()
    .UseJsonSerialization();

host.Build().Run();