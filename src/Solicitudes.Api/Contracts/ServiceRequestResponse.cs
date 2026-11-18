using Solicitudes.Core.Domain;

namespace Solicitudes.Api.Contracts;

public record ServiceRequestResponse(
    int Id,
    string ExternalReference,
    string CustomerId,
    string ServiceCode,
    string Description,
    RequestStatus Status,
    int Attempts,
    string? LastError,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static ServiceRequestResponse From(ServiceRequest request)
    {
        return new ServiceRequestResponse(
            request.Id,
            request.ExternalReference,
            request.CustomerId,
            request.ServiceCode,
            request.Description,
            request.Status,
            request.Attempts,
            request.LastError,
            request.CreatedAt,
            request.UpdatedAt);
    }
}