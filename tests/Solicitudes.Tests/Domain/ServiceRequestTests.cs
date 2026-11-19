using Solicitudes.Core.Domain;

namespace Solicitudes.Tests.Domain;

public class ServiceRequestTests
{
    [Fact]
    public void StartProcessing_LanzaExcepcion_CuandoLaSolicitudEstaCompletada()
    {
        var request = new ServiceRequest("REF-1", "CLI-1", "SRV-1", "Prueba");
        request.StartProcessing();
        request.Complete();

        Assert.Throws<InvalidOperationException>(() => request.StartProcessing());
        Assert.Equal(RequestStatus.Completed, request.Status);
    }
}