using Microsoft.EntityFrameworkCore;
using Solicitudes.Core.Data;
using Solicitudes.Core.Domain;

namespace Solicitudes.Core.Services;

public enum CreateOutcome
{
    Created,
    AlreadyExists,
    Conflict
}

public record CreateResult(CreateOutcome Outcome, ServiceRequest Request);

public class ServiceRequestService
{
    private readonly SolicitudesDbContext _db;

    public ServiceRequestService(SolicitudesDbContext db)
    {
        _db = db;
    }

    public async Task<CreateResult> CreateAsync(string externalReference, string customerId, string serviceCode, string description, CancellationToken cancellationToken)
    {
        externalReference = externalReference.Trim();

        var existing = await FindByReferenceAsync(externalReference, cancellationToken);
        if (existing is not null)
        {
            return CompareWithExisting(existing, customerId, serviceCode, description);
        }

        var request = new ServiceRequest(externalReference, customerId.Trim(), serviceCode.Trim(), description.Trim());
        _db.ServiceRequests.Add(request);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return new CreateResult(CreateOutcome.Created, request);
        }
        catch (DbUpdateException)
        {
            // Dos peticiones con la misma referencia al mismo tiempo: el índice único deja pasar solo una
            _db.Entry(request).State = EntityState.Detached;

            var winner = await FindByReferenceAsync(externalReference, cancellationToken);
            if (winner is null)
            {
                throw;
            }

            return CompareWithExisting(winner, customerId, serviceCode, description);
        }
    }

    public async Task<List<ServiceRequest>> GetByStatusAsync(RequestStatus? status, CancellationToken cancellationToken)
    {
        var query = _db.ServiceRequests.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<ServiceRequest?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    private async Task<ServiceRequest?> FindByReferenceAsync(string externalReference, CancellationToken cancellationToken)
    {
        return await _db.ServiceRequests.AsNoTracking().FirstOrDefaultAsync(r => r.ExternalReference == externalReference, cancellationToken);
    }

    private static CreateResult CompareWithExisting(ServiceRequest existing, string customerId, string serviceCode, string description)
    {
        var isSameRequest = existing.CustomerId == customerId.Trim()
            && existing.ServiceCode == serviceCode.Trim()
            && existing.Description == description.Trim();

        if (isSameRequest)
        {
            return new CreateResult(CreateOutcome.AlreadyExists, existing);
        }

        return new CreateResult(CreateOutcome.Conflict, existing);
    }
}