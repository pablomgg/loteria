using System.Text.Json.Serialization;
using Loteria.Domain.Enums;
using Loteria.Domain.Extensions;

namespace Loteria.Domain.Dtos.Lotofacil;

public class Lotofacil
{
    [JsonPropertyName("numero")]
    public int NumeroConcurso { get; init; }

    [JsonPropertyName("dataApuracao")]
    public string? DataApuracao { get; init; }

    [JsonPropertyName("listaDezenas")]
    public List<string>? ListaDezenas { get; init; }

    [JsonPropertyName("dezenasSorteadasOrdemSorteio")]
    public List<string>? DezenasSorteadasOrdemSorteio { get; init; }

    [JsonPropertyName("acumulado")]
    public bool Acumulado { get; init; }   
    
    [JsonPropertyName("indicadorConcursoEspecial")]
    public int IndicadorConcursoEspecial { get; init; }
    
    [JsonPropertyName("listaMunicipioUFGanhadores")]
    public required IReadOnlyCollection<Ganhador> Ganhadores { get; init; }
        
    [JsonPropertyName("listaRateioPremio")]
    public required IReadOnlyCollection<RateioPremio> RateioPremio { get; init; }
    
    [JsonPropertyName("localSorteio")]
    public string? LocalSorteio { get; init; }   
    
    [JsonPropertyName("nomeMunicipioUFSorteio")]
    public string? MunicipioUFSorteio { get; init; }    
    
    [JsonPropertyName("observacao")]
    public string? Observacao { get; init; }    
    
    [JsonPropertyName("valorArrecadado")]
    public decimal? ValorArrecadado { get; init; }
    
    public static implicit operator Concurso(Lotofacil dto)
    {
        var dezenas = dto.DezenasSorteadasOrdemSorteio ?? dto.ListaDezenas;
        
        var concurso = new Concurso
        {
            TipoJogo = TipoJogoLoteria.Lotofacil,
            NumeroConcurso = dto.NumeroConcurso,
            DataApuracao = DateExtensions.ParseBrazilDate(dto.DataApuracao),
            LocalSorteio = dto.LocalSorteio,
            MunicipioUFSorteio = dto.MunicipioUFSorteio,

            Lotofacil = new LotofacilDetalhe
            {
                Acumulado = dto.Acumulado,
                IndicadorConcursoEspecial = dto.IndicadorConcursoEspecial,
                Observacao = dto.Observacao,
                ValorArrecadado = dto.ValorArrecadado
            },
            
            Numeros = dezenas?
                .Select((d, i) => new ConcursoNumero
                {
                    Numero = short.Parse(d),
                    Posicao = dto.DezenasSorteadasOrdemSorteio != null
                        ? (byte?)(i + 1)
                        : null
                })
                .ToList() ?? [],

            Premios = dto.RateioPremio
                .Select(x => (ConcursoPremio)x)
                .ToList(),

            GanhadoresPorLocal = dto.Ganhadores
                .Select(x => (ConcursoGanhadorLocal)x)
                .ToList()
        };

        return concurso;
    }
}