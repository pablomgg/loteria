using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Loteria.Console.Configurations.OpenTelemetry;
using Loteria.Data;
using Loteria.Domain;
using Loteria.Domain.Enums;
using Loteria.Domain.Dtos.Lotofacil;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Loteria.Console.Hosts;

public class ImportLotofacilWorker(ILogger<ImportLotofacilWorker> logger,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime lifetime) : BackgroundService
{
    const string BaseUrl = "https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil";
    private readonly JsonSerializerOptions _json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var activity = Telemetry.Source.StartActivity("ImportLotofacilWorker.Run", ActivityKind.Internal);
            activity?.SetTag("worker.name", nameof(ImportLotofacilWorker));
                
            logger.LogInformation("===============> Iniciando worker <===============\n");
                
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            await LotofacilImporter(db, stoppingToken);
            
            logger.LogInformation("===============> Finalizando worker <===============\n");
                
            await Task.Delay(5000, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Worker cancelado.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha na importacao." + ex.Message);
        }
        finally
        {
            logger.LogInformation("Application is shutting down...");
            lifetime.StopApplication();
        }
    }
    
    private async Task LotofacilImporter(AppDbContext db, CancellationToken ct)
    {
        //TODO 1 Remover apos importacao, vai obter direto da api depois
        var dataPath = Path.Combine(AppContext.BaseDirectory, "data");
        
        if (!Directory.Exists(dataPath))
            throw new DirectoryNotFoundException($"Pasta não encontrada: {dataPath}");

        var files = Directory.GetFiles(dataPath, "lotofacil_*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        logger.LogInformation($"📦 Arquivos encontrados: {files.Count}");
        if (files.Count == 0) return;

        var raw = new List<Lotofacil>();

        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var list = JsonSerializer.Deserialize<List<Lotofacil>>(json, _json) ?? new();

                logger.LogInformation($"OK: {Path.GetFileName(file)} -> {list.Count} concursos");
                raw.AddRange(list);
            }
            catch (Exception ex)
            {
                logger.LogError($"ERRO lendo {Path.GetFileName(file)}: {ex.Message}");
            }
        }
        
        //TODO 1 Fim -------------
        
        var merged = raw
            .GroupBy(x => x.NumeroConcurso)
            .Select(g => g.OrderByDescending(x => ParseBrazilDate(x.DataApuracao) ?? DateTime.MinValue).First())
            .OrderBy(x => x.NumeroConcurso)
            .ToList();
        
        var totalInsert = 0;
        var totalUpdate = 0;
        
        // transação grande: se preferir, dá pra commitar em lotes (ex: 200 em 200)
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        foreach (var dto in merged)
        {
            ct.ThrowIfCancellationRequested();

            var tipo = TipoJogoLoteria.Lotofacil;

            var existente = await db.Set<Concurso>()
                .AsTracking()
                .Where(x => x.TipoJogo == tipo && x.NumeroConcurso == dto.NumeroConcurso)
                .Select(x => new { x.Id })
                .FirstOrDefaultAsync(ct);

            if (existente is null)
            {
                var entity = MapConcursoNovo(dto, tipo);

                db.Set<Concurso>().Add(entity);
                await db.SaveChangesAsync(ct);

                totalInsert++;
            }
            else
            {
                await UpdateConcursoAsync(db, existente.Id, dto, tipo, ct);
                totalUpdate++;
            }

            // log simples a cada N concursos
            if ((totalInsert + totalUpdate) % 200 == 0)
                logger.LogInformation($"... processados: {totalInsert + totalUpdate}");
        }

        await tx.CommitAsync(ct);

        logger.LogInformation($"✅ Import concluído!");
        logger.LogInformation($"Inseridos: {totalInsert} | Atualizados: {totalUpdate}");
    }
    
    private Concurso MapConcursoNovo(Lotofacil dto, TipoJogoLoteria tipo)
    {
        var concurso = new Concurso
        {
            TipoJogo = tipo,
            NumeroConcurso = dto.NumeroConcurso,
            DataApuracao = ParseBrazilDate(dto.DataApuracao),
            LocalSorteio = dto.LocalSorteio,
            MunicipioUFSorteio = dto.MunicipioUFSorteio,
            Lotofacil = new LotofacilDetalhe
            {
                Acumulado = dto.Acumulado,
                IndicadorConcursoEspecial = dto.IndicadorConcursoEspecial,
                Observacao = dto.Observacao,
                ValorArrecadado = dto.ValorArrecadado
            }
        };

        // dezenas: prioriza ordem do sorteio
        var dezenasSorteio = ParseDezenas(dto.DezenasSorteadasOrdemSorteio);
        var dezenasLista = ParseDezenas(dto.ListaDezenas);

        if (dezenasSorteio.Count > 0)
        {
            for (int i = 0; i < dezenasSorteio.Count; i++)
            {
                concurso.Numeros.Add(new ConcursoNumero
                {
                    Numero = dezenasSorteio[i],
                    Posicao = (byte)(i + 1)
                });
            }
        }
        else
        {
            // sem ordem de sorteio: guarda só Numero (Posicao null)
            foreach (var n in dezenasLista.Distinct().OrderBy(x => x))
                concurso.Numeros.Add(new ConcursoNumero { Numero = n, Posicao = null });
        }

        // rateio
        if (dto.RateioPremio is not null)
        {
            foreach (var r in dto.RateioPremio)
            {
                concurso.Premios.Add(new ConcursoPremio
                {
                    Faixa = (int)r.Faixa,
                    DescricaoFaixa = r.DescricaoFaixa,
                    NumeroDeGanhadores = r.NumeroDeGanhadores,
                    ValorPremio = r.ValorPremio
                });
            }
        }

        // ganhadores por local
        if (dto.Ganhadores is not null)
        {
            foreach (var g in dto.Ganhadores)
            {
                concurso.GanhadoresPorLocal.Add(new ConcursoGanhadorLocal
                {
                    Quantidade = g.Quantidade,
                    Municipio = string.IsNullOrWhiteSpace(g.Municipio) ? null : g.Municipio,
                    Uf = g.Uf
                });
            }
        }

        return concurso;
    }
    
    private async Task UpdateConcursoAsync(AppDbContext db, long concursoId, Lotofacil dto, TipoJogoLoteria tipo, CancellationToken ct)
    {
        // Atualiza o "pai"
        var concurso = await db.Set<Concurso>()
            .Include(x => x.Lotofacil)
            .FirstAsync(x => x.Id == concursoId, ct);

        concurso.DataApuracao = ParseBrazilDate(dto.DataApuracao);
        concurso.LocalSorteio = dto.LocalSorteio;
        concurso.MunicipioUFSorteio = dto.MunicipioUFSorteio;

        // detalhes lotofácil (1:1)
        if (concurso.Lotofacil is null)
        {
            concurso.Lotofacil = new LotofacilDetalhe { ConcursoId = concursoId };
        }

        concurso.Lotofacil.Acumulado = dto.Acumulado;
        concurso.Lotofacil.IndicadorConcursoEspecial = dto.IndicadorConcursoEspecial;
        concurso.Lotofacil.Observacao = dto.Observacao;
        concurso.Lotofacil.ValorArrecadado = dto.ValorArrecadado;

        // limpa filhos (rápido) e recria
        await db.Set<ConcursoNumero>().Where(x => x.ConcursoId == concursoId).ExecuteDeleteAsync(ct);
        await db.Set<ConcursoPremio>().Where(x => x.ConcursoId == concursoId).ExecuteDeleteAsync(ct);
        await db.Set<ConcursoGanhadorLocal>().Where(x => x.ConcursoId == concursoId).ExecuteDeleteAsync(ct);

        // dezenas
        var dezenasSorteio = ParseDezenas(dto.DezenasSorteadasOrdemSorteio);
        var dezenasLista = ParseDezenas(dto.ListaDezenas);

        if (dezenasSorteio.Count > 0)
        {
            var nums = new List<ConcursoNumero>(dezenasSorteio.Count);
            for (int i = 0; i < dezenasSorteio.Count; i++)
            {
                nums.Add(new ConcursoNumero
                {
                    ConcursoId = concursoId,
                    Numero = dezenasSorteio[i],
                    Posicao = (byte)(i + 1)
                });
            }
            db.Set<ConcursoNumero>().AddRange(nums);
        }
        else
        {
            var nums = dezenasLista
                .Distinct()
                .OrderBy(x => x)
                .Select(n => new ConcursoNumero
                {
                    ConcursoId = concursoId,
                    Numero = n,
                    Posicao = null
                });

            db.Set<ConcursoNumero>().AddRange(nums);
        }

        // rateio
        if (dto.RateioPremio is not null)
        {
            db.Set<ConcursoPremio>().AddRange(dto.RateioPremio.Select(r => new ConcursoPremio
            {
                ConcursoId = concursoId,
                Faixa = (int)r.Faixa,
                DescricaoFaixa = r.DescricaoFaixa,
                NumeroDeGanhadores = r.NumeroDeGanhadores,
                ValorPremio = r.ValorPremio
            }));
        }

        // ganhadores
        if (dto.Ganhadores is not null)
        {
            db.Set<ConcursoGanhadorLocal>().AddRange(dto.Ganhadores.Select(g => new ConcursoGanhadorLocal
            {
                ConcursoId = concursoId,
                Quantidade = g.Quantidade,
                Municipio = string.IsNullOrWhiteSpace(g.Municipio) ? null : g.Municipio,
                Uf = g.Uf
            }));
        }

        await db.SaveChangesAsync(ct);
    }
    
    private static List<short> ParseDezenas(List<string>? dezenas)
    {
        if (dezenas is null || dezenas.Count == 0) return new List<short>(0);

        var list = new List<short>(dezenas.Count);
        foreach (var s in dezenas)
        {
            if (short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                list.Add(n);
        }
        return list;
    }

    private static DateTime? ParseBrazilDate(string? ddMMyyyy)
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
}




// const string BaseUrl = "https://servicebus3.caixa.gov.br/portaldeloterias/api/lotofacil";
//
// var now = DateTimeOffset.Now;
// var startDate = now.AddMonths(1).Date;
// var endDate = now.Date;
//
// Console.WriteLine($"Coletando Lotofácil de {startDate:yyyy-MM-dd} até {endDate:yyyy-MM-dd}");
//
// using var http = CreateHttpClient();
//
// // 1) Último concurso
// // var latest = await GetJsonWithRetry<Lotofacil>(http, $"{BaseUrl}/");
// //
// // if (latest is null)
// // {
// //     Console.WriteLine("Erro ao obter último concurso.");
// //     return;
// // }
// //
// // var latestDate = ParseBrazilDate(latest.DataApuracao);
// // Console.WriteLine($"Último concurso: {latest.NumeroConcurso} - {latest.DataApuracao}");
// //
// var results = new List<Lotofacil>();
// //
// // if (latestDate is not null &&
// //     latestDate.Value.Date >= startDate &&
// //     latestDate.Value.Date <= endDate)
// // {
// //     results.Add(latest);
// // }
//
// // 2) Descer concursos
// //int current = latest.NumeroConcurso - 1;
// int current = 3621;
//
// const int BatchSize = 20;
// var batchCooldown = TimeSpan.FromSeconds(10);
// var okSinceCooldown = 0;
//
// while (current >= 1)
// {
//     await Task.Delay(TimeSpan.FromMilliseconds(850 + Random.Shared.Next(0, 650))); // 0,85s a 1,4s
//
//     var r = await GetJsonWithRetry<Lotofacil>(http, $"{BaseUrl}/{current}");
//     if (r is null)
//     {
//         current--;
//         continue;
//     }
//
//     var apuracao = ParseBrazilDate(r.DataApuracao);
//     if (apuracao is null)
//     {
//         current--;
//         continue;
//     }
//
//     if (apuracao.Value.Date >= startDate &&
//         apuracao.Value.Date <= endDate)
//     {
//         results.Add(r);
//         Console.WriteLine($"OK {r.NumeroConcurso} - {apuracao:yyyy-MM-dd}");
//         
//         okSinceCooldown++;
//
//         if (okSinceCooldown >= BatchSize)
//         {
//             Console.WriteLine($"Pausa de {batchCooldown.TotalSeconds}s para evitar 429 (após {BatchSize} OKs)...");
//             await Task.Delay(batchCooldown);
//             okSinceCooldown = 0;
//         }
//     }
//     else if (apuracao.Value.Date < startDate)
//     {
//         Console.WriteLine("Período finalizado.");
//         break;
//     }
//
//     current--;
// }
//
// // Ordenar por data
// results = results
//     .OrderBy(r => ParseBrazilDate(r.DataApuracao))
//     .ToList();
//
// // Criar pasta dentro do projeto
// var folder = Path.Combine(AppContext.BaseDirectory, "data");
// Directory.CreateDirectory(folder);
//
// var timestamp = DateTime.UtcNow.ToString("HHmmss");
//
// var filePath = Path.Combine(folder,
//     $"lotofacil_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{timestamp}.json");
//
// await File.WriteAllTextAsync(
//     filePath,
//     JsonSerializer.Serialize(results, GetJsonOptions()));
//
// Console.WriteLine();
// Console.WriteLine($"✅ Total concursos coletados: {results.Count}");
// Console.WriteLine($"📁 JSON salvo em: {filePath}");
//
//
// // =====================
// // Helpers
// // =====================
//
// static HttpClient CreateHttpClient()
// {
//     var handler = new HttpClientHandler
//     {
//         AutomaticDecompression =
//             DecompressionMethods.GZip |
//             DecompressionMethods.Deflate |
//             DecompressionMethods.Brotli
//     };
//
//     var http = new HttpClient(handler)
//     {
//         Timeout = TimeSpan.FromSeconds(60)
//     };
//
//     // User-Agent realista
//     http.DefaultRequestHeaders.UserAgent.ParseAdd(
//         "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
//
//     // Accept mais completo
//     http.DefaultRequestHeaders.Accept.Clear();
//     http.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
//
//     // Linguagem (muito importante pra WAF)
//     http.DefaultRequestHeaders.TryAddWithoutValidation(
//         "Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
//
//     // Simula requisição de navegador
//     http.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Dest", "empty");
//     http.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Mode", "cors");
//     http.DefaultRequestHeaders.TryAddWithoutValidation("Sec-Fetch-Site", "same-site");
//
//     http.DefaultRequestHeaders.Referrer =
//         new Uri("https://loterias.caixa.gov.br");
//
//     http.DefaultRequestHeaders.TryAddWithoutValidation("Origin",
//         "https://loterias.caixa.gov.br");
//
//     http.DefaultRequestHeaders.ConnectionClose = false;
//
//     return http;
// }
//
// static async Task<T?> GetJsonWithRetry<T>(HttpClient http, string url)
// {
//      // backoff exponencial base (ms)
//     var baseDelayMs = 800;
//     var maxAttempts = 8;
//
//     for (int attempt = 1; attempt <= maxAttempts; attempt++)
//     {
//         try
//         {
//             using var req = new HttpRequestMessage(HttpMethod.Get, url);
//             using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
//
//             if (resp.StatusCode == HttpStatusCode.NotFound)
//                 return default;
//             
//             if (resp.StatusCode == HttpStatusCode.Forbidden)
//             {
//                 var wait = TimeSpan.FromMinutes(5); // bem maior que 60s
//                 Console.WriteLine($"403 Forbidden. Provável bloqueio anti-bot. Aguardando {wait.TotalMinutes} min e tentando novamente...");
//                 await Task.Delay(wait);
//                 continue;
//             }
//
//             if ((int)resp.StatusCode == 429)
//             {
//                 var wait = TimeSpan.FromSeconds(60); // ou 60
//                 Console.WriteLine($"429 Too Many Requests. Aguardando {wait.TotalSeconds}s...");
//                 await Task.Delay(wait);
//                 continue;
//             }
//
//             if (!resp.IsSuccessStatusCode)
//             {
//                 // Se for 5xx, tenta de novo com backoff
//                 if ((int)resp.StatusCode >= 500 && attempt < maxAttempts)
//                 {
//                     var wait = TimeSpan.FromMilliseconds(
//                         Math.Min(30_000, baseDelayMs * Math.Pow(2, attempt - 1)))
//                         + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 350));
//
//                     Console.WriteLine($"HTTP {(int)resp.StatusCode}. Retry em {wait.TotalSeconds:F1}s...");
//                     await Task.Delay(wait);
//                     continue;
//                 }
//
//                 // outros erros: não fica insistindo
//                 Console.WriteLine($"HTTP {(int)resp.StatusCode} - {resp.ReasonPhrase} em {url}");
//                 return default;
//             }
//
//             await using var stream = await resp.Content.ReadAsStreamAsync();
//             return await JsonSerializer.DeserializeAsync<T>(stream, GetJsonOptions());
//         }
//         catch (Exception ex) when (attempt < maxAttempts)
//         {
//             var wait = TimeSpan.FromMilliseconds(
//                 Math.Min(20_000, baseDelayMs * Math.Pow(2, attempt - 1)))
//                 + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 350));
//
//             Console.WriteLine($"Erro: {ex.Message}. Retry em {wait.TotalSeconds:F1}s...");
//             await Task.Delay(wait);
//         }
//     }
//
//     return default;
// }
//
// static DateTime? ParseBrazilDate(string? ddMMyyyy)
// {
//     if (string.IsNullOrWhiteSpace(ddMMyyyy))
//         return null;
//
//     if (DateTime.TryParseExact(
//         ddMMyyyy,
//         "dd/MM/yyyy",
//         CultureInfo.GetCultureInfo("pt-BR"),
//         DateTimeStyles.None,
//         out var dt))
//         return dt;
//
//     return null;
// }
//
// static JsonSerializerOptions GetJsonOptions() => new()
// {
//     PropertyNameCaseInsensitive = true,
//     WriteIndented = true
// };