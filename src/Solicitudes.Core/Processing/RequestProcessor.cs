using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Solicitudes.Core.Data;
using Solicitudes.Core.Domain;
using Solicitudes.Core.Providers;

namespace Solicitudes.Core.Processing;

public class RequestProcessor
{
    private readonly SolicitudesDbContext _db;
    private readonly IProviderClient _provider;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly ILogger<RequestProcessor> _logger;

    public RequestProcessor(SolicitudesDbContext db, IProviderClient provider, ResiliencePipeline retryPipeline, ILogger<RequestProcessor> logger)
    {
        _db = db;
        _provider = provider;
        _retryPipeline = retryPipeline;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        var pendingIds = await _db.ServiceRequests
            .AsNoTracking()
            .Where(r => r.Status == RequestStatus.Pending)
            .OrderBy(r => r.CreatedAt)
            .Select(r => r.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processed = 0;

        foreach (var id in pendingIds)
        {
            if (await ProcessAsync(id, cancellationToken))
            {
                processed++;
            }
        }

        return processed;
    }

    public async Task<bool> ProcessAsync(int id, CancellationToken cancellationToken)
    {
        var request = await _db.ServiceRequests.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request is null || request.Status != RequestStatus.Pending)
        {
            return false;
        }

        request.StartProcessing();

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogInformation("La solicitud {Id} ya fue tomada por otro proceso", id);
            _db.ChangeTracker.Clear();
            return false;
        }

        try
        {
            await _retryPipeline.ExecuteAsync(async token =>
            {
                await _provider.SendAsync(request.ExternalReference, request.ServiceCode, token);
            }, cancellationToken);

            request.Complete();
            _logger.LogInformation("Solicitud {Reference} completada", request.ExternalReference);
        }
        catch (TransientProviderException ex)
        {
            request.Fail($"El proveedor falló después de varios reintentos: {ex.Message}");
            _logger.LogWarning("Solicitud {Reference} falló: {Error}", request.ExternalReference, ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}