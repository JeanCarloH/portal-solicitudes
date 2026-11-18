namespace Solicitudes.Core.Domain;

public class ServiceRequest
{
    public int Id { get; private set; }
    public string ExternalReference { get; private set; } = string.Empty;
    public string CustomerId { get; private set; } = string.Empty;
    public string ServiceCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public RequestStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ServiceRequest()
    {
    }

    public ServiceRequest(string externalReference, string customerId, string serviceCode, string description)
    {
        ExternalReference = externalReference;
        CustomerId = customerId;
        ServiceCode = serviceCode;
        Description = description;
        Status = RequestStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void StartProcessing()
    {
        ChangeStatus(RequestStatus.Processing);
        Attempts++;
    }

    public void Complete()
    {
        ChangeStatus(RequestStatus.Completed);
        LastError = null;
    }

    public void Fail(string error)
    {
        ChangeStatus(RequestStatus.Failed);
        LastError = error;
    }

    private void ChangeStatus(RequestStatus next)
    {
        if (!IsValidTransition(Status, next))
        {
            throw new InvalidOperationException($"No se puede pasar de {Status} a {next}.");
        }

        Status = next;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsValidTransition(RequestStatus current, RequestStatus next)
    {
        return (current, next) switch
        {
            (RequestStatus.Pending, RequestStatus.Processing) => true,
            (RequestStatus.Processing, RequestStatus.Completed) => true,
            (RequestStatus.Processing, RequestStatus.Failed) => true,
            _ => false
        };
    }
}