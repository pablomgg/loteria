using Loteria.Console.Integrations.LoteriasCaixaRefit;
using Loteria.Data;
using Loteria.Domain;
using Loteria.Domain.Dtos.MegaSena;
using Loteria.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;

namespace Loteria.Console.Services;

public class MegaSenaService(
    ILogger<MegaSenaService> logger,
    IServiceScopeFactory scopeFactory,
    IMegaSenaApi megaSenaApi) : IJogoImportService
{
    private const int SaveBatchSize = 100;

    public TipoJogoLoteria Tipo => TipoJogoLoteria.MegaSena;

    public async Task ImportAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        var lastNumberDb = await GetLastMegaSenaAsync(db, cancellationToken);
        var lastApi = await GetLastMegaSenaApiAsync(cancellationToken);
        
        logger.LogInformation("MegaSena -> banco: {db} | api: {api}", lastNumberDb, lastApi?.NumeroConcurso);

        if (lastApi?.NumeroConcurso <= lastNumberDb)
        {
            logger.LogInformation("MegaSena já está atualizada.");
            return;
        }
        
        var start = lastNumberDb + 1;
        var end = lastApi?.NumeroConcurso ?? 0;

        logger.LogInformation("MegaSena -> importando de {start} até {end}", start, end);

        await ImportRangeAsync(db, start, end, cancellationToken);

        logger.LogInformation("MegaSena -> import finalizado.");
    }
    
    private async Task<int> GetLastMegaSenaAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var numeroConcurso = await db.Set<Concurso>()
            .Where(c => c.TipoJogo == TipoJogoLoteria.MegaSena)
            .MaxAsync(c => (int?)c.NumeroConcurso, cancellationToken) ?? 0;

        return numeroConcurso;
    }
    
    private async Task<Concurso?> GetLastMegaSenaApiAsync(CancellationToken cancellationToken)
    {
        try
        {
            var mega = await megaSenaApi.GetLatestAsync(cancellationToken);
            return mega;
        }
        catch (ApiException ex) when ((int)ex.StatusCode == 404)
        {
            logger.LogError("Falha ao obter ultimo concurso na api da caixa." + ex.Message);
            return null;
        }
    }
    
    private async Task ImportRangeAsync(AppDbContext db, int start, int end, CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;
        var processed = 0;
        
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            var batch = new List<Concurso>(SaveBatchSize);

            for (int concursoNumero = start; concursoNumero <= end; concursoNumero++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dto = await TryGetByNumberAsync(concursoNumero, cancellationToken);
                if (dto is null)
                {
                    logger.LogWarning("MegaSena concurso {n}: não encontrado (404?)", concursoNumero);
                    continue;
                }

                Concurso entity = dto;
                batch.Add(entity);

                if (batch.Count >= SaveBatchSize)
                {
                    var (batchInserted, batchUpdated) = await PersistBatchAsync(db, batch, cancellationToken);

                    inserted += batchInserted;
                    updated += batchUpdated;
                    processed += batch.Count;

                    logger.LogInformation(
                        "MegaSena -> lote salvo com {batchCount} concursos. Total processados: {p} (ins {i} | upd {u})",
                        batch.Count, processed, inserted, updated);

                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                var (batchInserted, batchUpdated) = await PersistBatchAsync(db, batch, cancellationToken);

                inserted += batchInserted;
                updated += batchUpdated;
                processed += batch.Count;

                logger.LogInformation(
                    "MegaSena -> lote final salvo com {batchCount} concursos. Total processados: {p} (ins {i} | upd {u})",
                    batch.Count, processed, inserted, updated);

                batch.Clear();
            }

            logger.LogInformation("Resumo MegaSena: processados {p} | inseridos {i} | atualizados {u}",
                processed, inserted, updated);
        }
        finally
        {
            db.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
        
    private async Task<(int inserted, int updated)> PersistBatchAsync(
        AppDbContext db,
        List<Concurso> batch,
        CancellationToken cancellationToken)
    {
        var inserted = 0;
        var updated = 0;

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var concurso in batch)
            {
                var result = await UpsertAsync(db, concurso, cancellationToken);

                if (result == UpsertResult.Inserted)
                    inserted++;
                else if (result == UpsertResult.Updated)
                    updated++;
            }

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            db.ChangeTracker.Clear();
        }

        return (inserted, updated);
    }

    private async Task<MegaSena?> TryGetByNumberAsync(int numero, CancellationToken cancellationToken)
    {
        try
        {
            return await megaSenaApi.GetByNumberAsync(numero, cancellationToken);
        }
        catch (ApiException ex) when ((int)ex.StatusCode == 404)
        {
            logger.LogError($"Falha ao obter concurso {numero} na api da caixa." + ex.Message);
            return null;
        }
    }

    private async Task<UpsertResult> UpsertAsync(AppDbContext db, Concurso incoming, CancellationToken cancellationToken)
    {
        var existingId = await FindExistingIdAsync(db, incoming.TipoJogo, incoming.NumeroConcurso, cancellationToken);

        if (existingId is null)
        {
            await InsertAsync(db, incoming);
            return UpsertResult.Inserted;
        }

        await UpdateAsync(db, existingId.Value, incoming, cancellationToken);
        return UpsertResult.Updated;
    }

    private Task<long?> FindExistingIdAsync(AppDbContext db, TipoJogoLoteria tipo, int numero, CancellationToken cancellationToken)
        => db.Set<Concurso>()
            .Where(x => x.TipoJogo == tipo && x.NumeroConcurso == numero)
            .Select(x => (long?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private Task InsertAsync(AppDbContext db, Concurso entity)
    {
        db.Set<Concurso>().Add(entity);
        return Task.CompletedTask;
    }

    private async Task UpdateAsync(AppDbContext db, long id, Concurso incoming, CancellationToken cancellationToken)
    {
        var current = await db.Set<Concurso>()
            .Include(x => x.MegaSena)
            .FirstAsync(x => x.Id == id, cancellationToken);

        current.DataApuracao = incoming.DataApuracao;
        current.LocalSorteio = incoming.LocalSorteio;
        current.MunicipioUFSorteio = incoming.MunicipioUFSorteio;
        
        if (incoming.MegaSena is not null)
        {
            if (current.MegaSena is null)
                current.MegaSena = new MegaSenaDetalhe { ConcursoId = id };

            current.MegaSena.Acumulado = incoming.MegaSena.Acumulado;
            current.MegaSena.IndicadorConcursoEspecial = incoming.MegaSena.IndicadorConcursoEspecial;
            current.MegaSena.Observacao = incoming.MegaSena.Observacao;
            current.MegaSena.ValorArrecadado = incoming.MegaSena.ValorArrecadado;
        }
        
        await ReplaceChildrenAsync(db, id, incoming, cancellationToken);
    }

    private async Task ReplaceChildrenAsync(AppDbContext db, long concursoId, Concurso incoming, CancellationToken cancellationToken)
    {
        await db.Set<ConcursoNumero>().Where(x => x.ConcursoId == concursoId).ExecuteDeleteAsync(cancellationToken);
        await db.Set<ConcursoPremio>().Where(x => x.ConcursoId == concursoId).ExecuteDeleteAsync(cancellationToken);
        await db.Set<ConcursoGanhadorLocal>().Where(x => x.ConcursoId == concursoId).ExecuteDeleteAsync(cancellationToken);
        
        if (incoming.Numeros?.Count > 0)
        {
            foreach (var n in incoming.Numeros)
                n.ConcursoId = concursoId;

            db.Set<ConcursoNumero>().AddRange(incoming.Numeros);
        }

        if (incoming.Premios?.Count > 0)
        {
            foreach (var p in incoming.Premios)
                p.ConcursoId = concursoId;

            db.Set<ConcursoPremio>().AddRange(incoming.Premios);
        }

        if (incoming.GanhadoresPorLocal?.Count > 0)
        {
            foreach (var g in incoming.GanhadoresPorLocal)
                g.ConcursoId = concursoId;

            db.Set<ConcursoGanhadorLocal>().AddRange(incoming.GanhadoresPorLocal);
        }
    }

    private enum UpsertResult { Inserted, Updated, Noop }
}