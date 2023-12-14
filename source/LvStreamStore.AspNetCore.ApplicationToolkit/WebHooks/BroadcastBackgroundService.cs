namespace LvStreamStore.ApplicationToolkit.WebHooks;

using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

internal class BroadcastBackgroundService : BackgroundService {
    private readonly ILogger _logger;
    private readonly IStreamStoreRepository _streamStoreRepository;
    private readonly IHttpClientFactory _httpClientFactory;

    public BroadcastBackgroundService(ILoggerFactory loggerFactory, IStreamStoreRepository streamStoreRepository, IHttpClientFactory httpClientFactory) {
        _logger = loggerFactory.CreateLogger<BroadcastBackgroundService>();
        _streamStoreRepository = streamStoreRepository;
        _httpClientFactory = httpClientFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        throw new NotImplementedException();
    }
}
