using Solicitudes.Core.Providers;

namespace Solicitudes.Tests.Support;

public class FakeProviderClient : IProviderClient
{
    private readonly int _failuresBeforeSuccess;

    public FakeProviderClient(int failuresBeforeSuccess)
    {
        _failuresBeforeSuccess = failuresBeforeSuccess;
    }

    public int Calls { get; private set; }

    public Task SendAsync(string externalReference, string serviceCode, CancellationToken cancellationToken)
    {
        Calls++;

        if (Calls <= _failuresBeforeSuccess)
        {
            throw new TransientProviderException("Fallo de prueba");
        }

        return Task.CompletedTask;
    }
}