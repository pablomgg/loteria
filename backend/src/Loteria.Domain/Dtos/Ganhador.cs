using System.Text.Json.Serialization;

namespace Loteria.Domain.Dtos;

public class Ganhador
{
    [JsonPropertyName("ganhadores")]
    public required int Quantidade { get; init; }
    
    [JsonPropertyName("municipio")]
    public required string Municipio { get; init; }
    
    [JsonPropertyName("uf")]
    public required string Uf { get; init; }
    
    public static explicit operator ConcursoGanhadorLocal(Ganhador dto)
    {
        return new ConcursoGanhadorLocal
        {
            Quantidade = dto.Quantidade,
            Municipio = string.IsNullOrWhiteSpace(dto.Municipio) ? null : dto.Municipio,
            Uf = dto.Uf
        };
    }
}