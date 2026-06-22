namespace Loteria.Domain;

public class MegaSenaDetalhe
{
    public long ConcursoId { get; set; }
    public bool Acumulado { get; set; }
    public decimal? ValorArrecadado { get; set; }
    public decimal? ValorAcumulado { get; set; }
    public int IndicadorConcursoEspecial { get; set; }
    public string? Observacao { get; set; }

    public Concurso Concurso { get; set; } = null!;

    public MegaSenaDetalhe()
    {
        
    }
}