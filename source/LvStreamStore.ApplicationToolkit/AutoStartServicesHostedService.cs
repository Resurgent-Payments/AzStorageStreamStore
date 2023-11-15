namespace LvStreamStore.ApplicationToolkit {
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    internal class AutoStartServicesHostedService : IHostedService {
        private readonly IServiceProvider _services;

        public AutoStartServicesHostedService(IServiceProvider services) {
            _services = services;
        }

        public Task StartAsync(CancellationToken cancellationToken) {
            _ = _services.GetServices<IAutoStartService>();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
