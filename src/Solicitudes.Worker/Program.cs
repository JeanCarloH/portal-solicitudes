using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;
using Solicitudes.Core.Data;
using Solicitudes.Core.Processing;
using Solicitudes.Core.Providers;
using Solicitudes.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Solicitudes Worker";
});

builder.Services.AddDbContext<SolicitudesDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("Default"));
});

builder.Services.AddSingleton<IProviderClient, SimulatedProviderClient>();

builder.Services.AddSingleton(new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions
    {
        ShouldHandle = new PredicateBuilder().Handle<TransientProviderException>(),
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    })
    .Build());

builder.Services.AddScoped<RequestProcessor>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SolicitudesDbContext>();
    db.Database.Migrate();
}

host.Run();