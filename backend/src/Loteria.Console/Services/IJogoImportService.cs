using Loteria.Domain.Enums;

namespace Loteria.Console.Services;

public interface IJogoImportService
{
    TipoJogoLoteria Tipo { get; }
    Task ImportAsync(CancellationToken cancellationToken);
}