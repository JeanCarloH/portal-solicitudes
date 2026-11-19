using Solicitudes.Core.Processing;

namespace Solicitudes.Worker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;

    public Worker(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(configuration.GetValue("Worker:IntervalSeconds", 5));
        _batchSize = configuration.GetValue("Worker:BatchSize", 10);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado, revisando cada {Interval} segundos", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<RequestProcessor>();
                await processor.ProcessPendingAsync(_batchSize, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error procesando solicitudes pendientes");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}