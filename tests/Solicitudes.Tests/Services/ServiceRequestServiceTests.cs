using Solicitudes.Core.Services;
using Solicitudes.Tests.Support;

namespace Solicitudes.Tests.Services;

public class ServiceRequestServiceTests
{
    [Fact]
    public async Task CreateAsync_DevuelveLaMismaSolicitud_CuandoSeReintentaConLaMismaReferencia()
    {
        using var database = new TestDatabase();
        await using var context = database.CreateContext();
        var service = new ServiceRequestService(context);

        var first = await service.CreateAsync("REF-1", "CLI-1", "SRV-1", "Prueba", CancellationToken.None);
        var second = await service.CreateAsync("REF-1", "CLI-1", "SRV-1", "Prueba", CancellationToken.None);

        Assert.Equal(CreateOutcome.Created, first.Outcome);
        Assert.Equal(CreateOutcome.AlreadyExists, second.Outcome);
        Assert.Equal(first.Request.Id, second.Request.Id);
        Assert.Equal(1, context.ServiceRequests.Count());
    }
}