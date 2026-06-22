namespace Loteria.Domain;

public class ConcursoGanhadorLocal
{
    public long Id { get; set; }
    public long ConcursoId { get; set; }

    public int Quantidade { get; set; }
    public string? Municipio { get; set; }
    public string Uf { get; set; } = null!;

    public Concurso Concurso { get; set; } = null!;

    public ConcursoGanhadorLocal()
    {
        
    }
}