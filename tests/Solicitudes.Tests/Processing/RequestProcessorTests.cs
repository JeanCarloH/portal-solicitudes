using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.Retry;
using Solicitudes.Core.Data;
using Solicitudes.Core.Domain;
using Solicitudes.Core.Processing;
using Solicitudes.Core.Providers;
using Solicitudes.Tests.Support;

namespace Solicitudes.Tests.Processing;

public class RequestProcessorTests
{
    [Fact]
    public async Task ProcessAsync_MarcaComoFallida_CuandoElProveedorFallaEnTodosLosIntentos()
    {
        using var database = new TestDatabase();
        var id = await SeedPendingRequestAsync(database);
        var providerMock = new FakeProviderClient(failuresBeforeSuccess: 10);

        await using (var context = database.CreateContext())
        {
            var processor = CreateProcessor(context, providerMock);
            await processor.ProcessAsync(id, CancellationToken.None);
        }

        var request = await FindAsync(database, id);
        Assert.Equal(RequestStatus.Failed, request.Status);
        Assert.NotNull(request.LastError);
        Assert.Equal(4, providerMock.Calls);
    }

    [Fact]
    public async Task ProcessAsync_NoLlamaAlProveedor_CuandoLaSolicitudYaFueProcesada()
    {
        using var database = new TestDatabase();
        var id = await SeedPendingRequestAsync(database);
        var providerMock = new FakeProviderClient(failuresBeforeSuccess: 0);

        await using (var context = database.CreateContext())
        {
            var processor = CreateProcessor(context, providerMock);
            await processor.ProcessAsync(id, CancellationToken.None);
        }

        bool processedAgain;
        await using (var context = database.CreateContext())
        {
            var processor = CreateProcessor(context, providerMock);
            processedAgain = await processor.ProcessAsync(id, CancellationToken.None);
        }

        Assert.False(processedAgain);
        Assert.Equal(1, providerMock.Calls);
    }

    private static RequestProcessor CreateProcessor(SolicitudesDbContext context, IProviderClient provider)
    {
        var retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<TransientProviderException>(),
                MaxRetryAttempts = 3,
                Delay = TimeSpan.Zero
            })
            .Build();

        return new RequestProcessor(context, provider, retryPipeline, NullLogger<RequestProcessor>.Instance);
    }

    private static async Task<int> SeedPendingRequestAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var request = new ServiceRequest("REF-1", "CLI-1", "SRV-1", "Prueba");
        context.ServiceRequests.Add(request);
        await context.SaveChangesAsync();
        return request.Id;
    }

    private static async Task<ServiceRequest> FindAsync(TestDatabase database, int id)
    {
        await using var context = database.CreateContext();
        return await context.ServiceRequests.FindAsync(id) ?? throw new InvalidOperationException("No existe la solicitud");
    }
}