using System.Text.Json.Serialization;

namespace Loteria.Domain.Dtos;

public class RateioPremio
{
    [JsonPropertyName("descricaoFaixa")]
    public required string DescricaoFaixa { get; init; }
    
    [JsonPropertyName("faixa")]
    public required int Faixa { get; init; }
    
    [JsonPropertyName("numeroDeGanhadores")]
    public required int NumeroDeGanhadores { get; init; }
    
    [JsonPropertyName("valorPremio")]
    public required decimal ValorPremio { get; init; }

    public static explicit operator ConcursoPremio(RateioPremio dto)
    {
        return new()
        {
            DescricaoFaixa = dto.DescricaoFaixa,
            Faixa = dto.Faixa,
            NumeroDeGanhadores = dto.NumeroDeGanhadores,
            ValorPremio = dto.ValorPremio
        };
    }
}