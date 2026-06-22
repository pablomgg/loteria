using Loteria.Domain.Dtos.Lotofacil;
using Refit;

namespace Loteria.Console.Integrations.LoteriasCaixaRefit;

public interface ILotofacilApi
{
    [Get("/lotofacil/")]
    Task<Lotofacil> GetLatestAsync(CancellationToken ct = default);
    
    [Get("/lotofacil/{numero}")]
    Task<Lotofacil> GetByNumberAsync(int numero, CancellationToken ct = default);
}