using System.Diagnostics;
using Loteria.Console.Configurations.OpenTelemetry;
using Loteria.Console.Services;
using Loteria.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loteria.Console.Hosts;

public class PoolingWorker(ILogger<PoolingWorker> logger,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var activity = Telemetry.Source.StartActivity("PoolingWorker.Run");
            activity?.SetTag("worker.name", nameof(PoolingWorker));
            
            await using var scope = scopeFactory.CreateAsyncScope();
            
            var dispatcher = scope.ServiceProvider.GetRequiredService<ILoteriaImportDispatcher>(); 
        
            await dispatcher.ImportAsync(TipoJogoLoteria.Lotofacil, stoppingToken);
            await dispatcher.ImportAsync(TipoJogoLoteria.MegaSena, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Worker cancelado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na importacao." + ex.Message);
        }
        finally
        {
            logger.LogInformation("Application is shutting down...");
            lifetime.StopApplication();
        }
    }
}