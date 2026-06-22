using Loteria.Domain.Dtos.MegaSena;
using Refit;

namespace Loteria.Console.Integrations.LoteriasCaixaRefit;

public interface IMegaSenaApi
{
    [Get("/megasena")]
    Task<MegaSena> GetLatestAsync(CancellationToken ct = default);

    [Get("/megasena/{numero}")]
    Task<MegaSena> GetByNumberAsync(int numero, CancellationToken ct = default);
}