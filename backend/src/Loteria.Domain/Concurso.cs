using Loteria.Domain.Enums;

namespace Loteria.Domain;

public class Concurso
{
    public long Id { get; set; } // surrogate key para facilitar FKs
    public TipoJogoLoteria TipoJogo { get; set; }

    public int NumeroConcurso { get; set; }
    public DateTime? DataApuracao { get; set; }

    public string? LocalSorteio { get; set; }
    public string? MunicipioUFSorteio { get; set; }

    // Navegações genéricas
    public ICollection<ConcursoNumero> Numeros { get; set; } = new List<ConcursoNumero>();
    public ICollection<ConcursoPremio> Premios { get; set; } = new List<ConcursoPremio>();
    public ICollection<ConcursoGanhadorLocal> GanhadoresPorLocal { get; set; } = new List<ConcursoGanhadorLocal>();

    // 1:1 com detalhes específicos (opcionais)
    public LotofacilDetalhe? Lotofacil { get; set; }
    public MegaSenaDetalhe? MegaSena { get; set; }

    public Concurso()
    {
        
    }
}