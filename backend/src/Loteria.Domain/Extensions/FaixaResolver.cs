using Loteria.Domain.Enums;

namespace Loteria.Domain.Extensions;

public static class FaixaResolver
{
    private static readonly IReadOnlyDictionary<TipoJogoLoteria, Func<int, Enum>> TiposFaixa =
        new Dictionary<TipoJogoLoteria, Func<int, Enum>>
        {
            [TipoJogoLoteria.Lotofacil] = faixa => (FaixaLotofacil)faixa,
            [TipoJogoLoteria.MegaSena] = faixa => (FaixaMegaSena)faixa
        };

    public static Enum Resolve(TipoJogoLoteria tipo, int faixa)
    {
        if (!TiposFaixa.TryGetValue(tipo, out var resolver))
            throw new NotSupportedException($"Tipo de jogo não suportado: {tipo}");

        return resolver(faixa);
    }
}