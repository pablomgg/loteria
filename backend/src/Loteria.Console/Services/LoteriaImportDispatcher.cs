using Loteria.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Loteria.Console.Services;

public sealed class LoteriaImportDispatcher(IServiceProvider sp) : ILoteriaImportDispatcher
{
    public Task ImportAsync(TipoJogoLoteria tipo, CancellationToken cancellationToken)
    {
        var services = sp.GetServices<IJogoImportService>();
        var service = services.FirstOrDefault(s => s.Tipo == tipo);

        if (service is null)
            throw new InvalidOperationException($"Nenhum import service registrado para {tipo}.");

        return service.ImportAsync(cancellationToken);
    }
}