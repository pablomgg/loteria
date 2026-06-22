using Loteria.Domain.Enums;

namespace Loteria.Console.Services;

public interface ILoteriaImportDispatcher
{
    Task ImportAsync(TipoJogoLoteria tipoJogoLoteria, CancellationToken cancellationToken);
}