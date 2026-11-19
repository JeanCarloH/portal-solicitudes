using System.Collections.Concurrent;
using Solicitudes.Core.Providers;

namespace Solicitudes.Worker;

public class SimulatedProviderClient : IProviderClient
{
    private readonly ConcurrentDictionary<string, bool> _processedReferences = new();
    private readonly double _failureRate;
    private readonly ILogger<SimulatedProviderClient> _logger;

    public SimulatedProviderClient(IConfiguration configuration, ILogger<SimulatedProviderClient> logger)
    {
        _failureRate = configuration.GetValue("Provider:FailureRate", 0.3);
        _logger = logger;
    }

    public async Task SendAsync(string externalReference, string serviceCode, CancellationToken cancellationToken)
    {
        await Task.Delay(Random.Shared.Next(200, 800), cancellationToken);

        if (_processedReferences.ContainsKey(externalReference))
        {
            _logger.LogInformation("El proveedor ya procesó {Reference}, no repite el efecto", externalReference);
            return;
        }

        if (Random.Shared.NextDouble() < _failureRate)
        {
            throw new TransientProviderException("Fallo transitorio simulado del proveedor");
        }

        _processedReferences.TryAdd(externalReference, true);
        _logger.LogInformation("Proveedor procesó {Reference} ({ServiceCode})", externalReference, serviceCode);
    }
}