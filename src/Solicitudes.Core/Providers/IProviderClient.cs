namespace Solicitudes.Core.Providers;

public interface IProviderClient
{
    Task SendAsync(string externalReference, string serviceCode, CancellationToken cancellationToken);
}

public class TransientProviderException : Exception
{
    public TransientProviderException(string message) : base(message)
    {
    }
}