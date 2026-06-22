using System.Globalization;
using System.Text.Json;
using Loteria.Domain.Dtos.Lotofacil;

if (!Directory.Exists(GetPath()))
{
    Console.WriteLine($"Pasta não encontrada: {GetPath()}");
    return;
}

var files = Directory.GetFiles(GetPath(), "lotofacil_*.json", SearchOption.TopDirectoryOnly)
                     .OrderBy(f => f)
                     .ToList();

Console.WriteLine($"Arquivos encontrados: {files.Count}");
if (files.Count == 0)
{
    Console.WriteLine("Nenhum arquivo lotofacil_*.json encontrado.");
    return;
}

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

var all = new List<Lotofacil>();

foreach (var file in files)
{
    try
    {
        var json = await File.ReadAllTextAsync(file);
        var list = JsonSerializer.Deserialize<List<Lotofacil>>(json, options) ?? new();

        Console.WriteLine($"OK: {Path.GetFileName(file)} -> {list.Count} concursos");
        all.AddRange(list);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ERRO lendo {Path.GetFileName(file)}: {ex.Message}");
    }
}

Console.WriteLine($"\nTotal bruto (com duplicados): {all.Count}");

// Dedup por número: escolhe o mais recente pela dataApuracao (se der pra parsear)
var merged = all
    .GroupBy(x => x.NumeroConcurso)
    .Select(g =>
        g.OrderByDescending(x => ParseBrazilDate(x.DataApuracao) ?? DateTime.MinValue)
         .First()
    )
    .OrderBy(x => x.NumeroConcurso)
    .ToList();

Console.WriteLine($"Total após dedup: {merged.Count}");

// (Opcional) salvar um "consolidado" pra você nunca mais sofrer com isso
//var mergedPath = Path.Combine(GetPath(), $"lotofacil_merged_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
//await File.WriteAllTextAsync(mergedPath, JsonSerializer.Serialize(merged, new JsonSerializerOptions { WriteIndented = true }));
//Console.WriteLine($"📌 Consolidado salvo em: {mergedPath}");


Console.WriteLine($"Concursos lidos: {all.Count}");

// 1) Frequência por dezena
var freq = all
    .SelectMany(c => c.ListaDezenas.Select(d => int.Parse(d)))
    .GroupBy(x => x)
    .Select(g => new { Dezena = g.Key, Qtde = g.Count() })
    .OrderByDescending(x => x.Qtde)
    .ThenBy(x => x.Dezena)
    .ToList();

Console.WriteLine("\nTop 15 dezenas (mais saíram):");
foreach (var x in freq.Take(15))
    Console.WriteLine($"{x.Dezena:00} -> {x.Qtde}");

// 2) Ímpar/Par e Baixo/Alto por concurso
var dist = all.Select(c =>
{
    var nums = c.ListaDezenas.Select(d => int.Parse(d)).OrderBy(n => n).ToArray();
    var odd = nums.Count(n => n % 2 == 1);
    var even = 15 - odd;
    var low = nums.Count(n => n <= 13);
    var high = 15 - low;

    // consecutivos
    var consec = 0;
    for (int i = 1; i < nums.Length; i++)
        if (nums[i] == nums[i - 1] + 1) consec++;

    return new { c.NumeroConcurso, odd, even, low, high, consec };
}).ToList();

Console.WriteLine("\nDistribuição ímpar/par (contagem de concursos por padrão):");
foreach (var g in dist.GroupBy(x => $"{x.odd}/{x.even}").OrderByDescending(g => g.Count()))
    Console.WriteLine($"{g.Key} -> {g.Count()}");

Console.WriteLine("\nDistribuição baixo(1-13)/alto(14-25):");
foreach (var g in dist.GroupBy(x => $"{x.low}/{x.high}").OrderByDescending(g => g.Count()))
    Console.WriteLine($"{g.Key} -> {g.Count()}");

Console.WriteLine("\nConsecutivos (quantos pares consecutivos por concurso):");
foreach (var g in dist.GroupBy(x => x.consec).OrderBy(g => g.Key))
    Console.WriteLine($"{g.Key} -> {g.Count()}");

// 3) Pares mais comuns (combinatória)
var pares = all
    .SelectMany(c =>
    {
        var nums = c.ListaDezenas.Select(d => int.Parse(d)).OrderBy(n => n).ToArray();
        var list = new List<(int a, int b)>();
        for (int i = 0; i < nums.Length; i++)
            for (int j = i + 1; j < nums.Length; j++)
                list.Add((nums[i], nums[j]));
        return list;
    })
    .GroupBy(p => p)
    .Select(g => new { Par = g.Key, Qtde = g.Count() })
    .OrderByDescending(x => x.Qtde)
    .Take(20)
    .ToList();

Console.WriteLine("\nTop 20 pares que mais se repetiram:");
foreach (var x in pares)
    Console.WriteLine($"{x.Par.a:00}-{x.Par.b:00} -> {x.Qtde}");


// ===============================
// ✅ Verificar seus jogos
// ===============================

// Seus 20 jogos (cada linha = 15 dezenas)
var myGames = new[]
{
    "03 04 05 06 07 11 12 13 14 15 19 20 23 24 25",
    "03 04 05 06 07 11 12 13 14 15 19 21 23 24 25",
    "03 04 05 06 07 11 12 13 15 19 20 21 23 24 25",
    "03 04 05 06 07 11 13 14 15 19 20 21 23 24 25",
    "03 04 05 06 07 11 12 13 14 15 20 21 23 24 25",
    "03 04 05 06 07 11 12 14 15 19 20 21 23 24 25",
    "03 04 05 06 11 12 13 14 15 19 20 21 23 24 25",
    "03 04 05 07 11 12 13 14 15 19 20 21 23 24 25",
    "03 04 06 07 11 12 13 14 15 19 20 21 23 24 25",
    "04 05 06 07 11 12 13 14 15 19 20 21 23 24 25",
    "03 05 06 07 11 12 13 14 15 19 20 21 23 24 25",
    "03 04 06 07 11 12 13 14 15 19 20 21 23 24 17",
    "03 04 05 07 11 12 13 14 15 19 20 21 23 24 16",
    "03 04 05 06 11 12 13 14 15 19 20 21 23 24 10",
    "03 04 05 06 07 12 13 14 15 19 20 21 23 24 25",
    "03 04 05 06 07 11 13 14 15 19 20 21 23 24 02",
    "03 04 05 06 07 11 12 14 15 19 20 21 23 24 09",
    "03 04 05 06 07 11 12 13 15 19 20 21 23 24 01",
    "03 04 05 06 07 11 12 13 14 19 20 21 23 24 25",
    "03 04 05 06 07 11 12 13 14 15 20 21 23 24 08",
};

// Indexa concursos por assinatura (as 15 dezenas ordenadas)
var contestBySignature = merged
    .Where(c => c.ListaDezenas is not null && c.ListaDezenas.Count == 15)
    .Select(c =>
    {
        var nums = c.ListaDezenas.Select(d => int.Parse(d)).OrderBy(n => n).ToArray();
        var signature = Signature(nums);

        return new
        {
            signature,
            c.NumeroConcurso,
            c.DataApuracao
        };
    })
    .GroupBy(x => x.signature)
    .ToDictionary(
        g => g.Key,
        g => g.Select(x => (x.NumeroConcurso, x.DataApuracao)).ToList()
    );

// Verifica seus jogos
Console.WriteLine();
Console.WriteLine("==================================");
Console.WriteLine("🔎 Conferindo seus jogos...");
Console.WriteLine("==================================");

var hits = 0;

for (int i = 0; i < myGames.Length; i++)
{
    var nums = ParseGame(myGames[i]);
    if (nums.Length != 15)
    {
        Console.WriteLine($"Jogo #{i + 1:00} inválido (não tem 15 dezenas): {myGames[i]}");
        continue;
    }

    var sig = Signature(nums);

    if (contestBySignature.TryGetValue(sig, out var contests))
    {
        hits++;
        Console.WriteLine($"✅ Jogo #{i + 1:00} JÁ SAIU ({contests.Count}x): {sig}");

        foreach (var (numero, data) in contests.OrderBy(x => x.NumeroConcurso))
            Console.WriteLine($"   - Concurso {numero} ({data})");
    }
    else
    {
        Console.WriteLine($"❌ Jogo #{i + 1:00} nunca saiu: {sig}");
    }
}

Console.WriteLine();
Console.WriteLine($"📌 Resultado: {hits}/{myGames.Length} jogos seus já apareceram exatamente iguais no histórico.");

Console.WriteLine();
Console.WriteLine("==================================");
Console.WriteLine("🎯 Match parcial (13 e 14 acertos)");
Console.WriteLine("==================================");

// Pré-processa concursos em HashSet pra ficar rápido
var contests2 = merged
    .Where(c => c.ListaDezenas is not null && c.ListaDezenas.Count == 15)
    .Select(c => new
    {
        c.NumeroConcurso,
        c.DataApuracao,
        Set = c.ListaDezenas.Select(d => int.Parse(d)).ToHashSet()
    })
    .ToList();

var total13 = 0;
var total14 = 0;

for (int i = 0; i < myGames.Length; i++)
{
    var gameNums = ParseGame(myGames[i]);
    if (gameNums.Length != 15)
    {
        Console.WriteLine($"Jogo #{i + 1:00} inválido (não tem 15 dezenas): {myGames[i]}");
        continue;
    }

    var gameSet = gameNums.ToHashSet();

    var hits13 = new List<(int concurso, string data, int acertos)>();
    var hits14 = new List<(int concurso, string data, int acertos)>();

    foreach (var c in contests2)
    {
        // interseção (acertos)
        var acertos = 0;
        foreach (var n in gameSet)
            if (c.Set.Contains(n)) acertos++;

        if (acertos == 14) hits14.Add((c.NumeroConcurso, c.DataApuracao, acertos));
        else if (acertos == 13) hits13.Add((c.NumeroConcurso, c.DataApuracao, acertos));
    }

    total13 += hits13.Count;
    total14 += hits14.Count;

    Console.WriteLine();
    Console.WriteLine($"Jogo #{i + 1:00}: {Signature(gameNums)}");
    Console.WriteLine($"  ✅ 14 acertos: {hits14.Count}");
    Console.WriteLine($"  ✅ 13 acertos: {hits13.Count}");

    // 🔽 Se quiser listar os concursos, deixe assim.
    // Se não quiser poluir o console, comente os blocos abaixo.

    if (hits14.Count > 0)
    {
        Console.WriteLine("  Detalhes 14:");
        foreach (var h in hits14.OrderBy(x => x.concurso))
            Console.WriteLine($"    - Concurso {h.concurso} ({h.data})");
    }

    if (hits13.Count > 0)
    {
        Console.WriteLine("  Detalhes 13:");
        foreach (var h in hits13.OrderBy(x => x.concurso))
            Console.WriteLine($"    - Concurso {h.concurso} ({h.data})");
    }
}

Console.WriteLine();
Console.WriteLine("==================================");
Console.WriteLine($"📌 Total geral no histórico -> 14 acertos: {total14} | 13 acertos: {total13}");
Console.WriteLine("==================================");



// =====================
// Helpers
// =====================
static DateTime? ParseBrazilDate(string? ddMMyyyy)
{
    if (string.IsNullOrWhiteSpace(ddMMyyyy))
        return null;

    if (DateTime.TryParseExact(
        ddMMyyyy,
        "dd/MM/yyyy",
        CultureInfo.GetCultureInfo("pt-BR"),
        DateTimeStyles.None,
        out var dt))
        return dt;

    return null;
}


static int[] ParseGame(string line)
{
    // aceita "03 04 ..." e também com múltiplos espaços
    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return parts.Select(p => int.Parse(p)).OrderBy(x => x).ToArray();
}

static string Signature(IEnumerable<int> nums)
{
    // assinatura fixa: "01 02 03 ... 25"
    return string.Join(' ', nums.Select(n => n.ToString("00")));
}

static string GetPath() => Path.Combine(AppContext.BaseDirectory, "data");

public record Concurso(int numero, string dataApuracao, string[] listaDezenas);
