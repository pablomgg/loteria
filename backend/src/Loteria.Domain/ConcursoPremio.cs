using Loteria.Domain.Enums;
using Loteria.Domain.Extensions;

namespace Loteria.Domain;

public class ConcursoPremio
{
    public long Id { get; set; }
    public long ConcursoId { get; set; }

    public int Faixa { get; set; }
    public string? DescricaoFaixa { get; set; }

    public int NumeroDeGanhadores { get; set; }
    public decimal ValorPremio { get; set; }

    public Concurso Concurso { get; set; } = null!;

    public ConcursoPremio()
    {
        
    }
    
    public Enum GetFaixaEnum(TipoJogoLoteria tipo)
    {
        return FaixaResolver.Resolve(tipo, Faixa);
    }
}