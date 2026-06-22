namespace Loteria.Domain;

public class LotofacilDetalhe
{
    public long ConcursoId { get; set; } // PK e FK
    public bool Acumulado { get; set; }
    public int IndicadorConcursoEspecial { get; set; }
    public string? Observacao { get; set; }
    public decimal? ValorArrecadado { get; set; }

    public Concurso Concurso { get; set; } = null!;

    public LotofacilDetalhe()
    {
        
    }
}