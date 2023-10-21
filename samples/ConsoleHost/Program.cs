using LvStreamStore;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var hostBuilder = Host.CreateDefaultBuilder(args);
hostBuilder.ConfigureServices(svc => svc.AddLvStreamStore<MemoryEventStream>((conf) => conf.RunBackgroundService = true));

hostBuilder.Build().Run();