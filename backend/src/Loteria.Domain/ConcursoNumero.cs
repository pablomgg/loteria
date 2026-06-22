namespace Loteria.Domain;

public class ConcursoNumero
{
    public long Id { get; set; }
    public long ConcursoId { get; set; }

    public short Numero { get; set; } // 1..60, 0..99 etc (depende do jogo)
    public byte? Posicao { get; set; } // ordem do sorteio (quando existir)

    public Concurso Concurso { get; set; } = null!;

    public ConcursoNumero()
    {
        
    }
}