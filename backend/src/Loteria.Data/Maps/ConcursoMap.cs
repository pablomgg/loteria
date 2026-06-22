using Loteria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loteria.Data.Maps;

public class ConcursoMap : IEntityTypeConfiguration<Concurso>
{
    public void Configure(EntityTypeBuilder<Concurso> b)
    {
        b.ToTable("Concurso", "dbo");
        b.HasKey(x => x.Id);

        b.Property(x => x.TipoJogo)
            .HasConversion<short>()
            .IsRequired();

        b.Property(x => x.NumeroConcurso)
            .IsRequired();

        b.Property(x => x.DataApuracao)
            .HasColumnType("date");

        b.Property(x => x.LocalSorteio)
            .HasMaxLength(120);

        b.Property(x => x.MunicipioUFSorteio)
            .HasMaxLength(120);

        b.HasIndex(x => new { x.TipoJogo, x.NumeroConcurso })
            .IsUnique()
            .HasDatabaseName("UX_Concurso_Tipo_Numero");

        b.HasMany(x => x.Numeros)
            .WithOne(x => x.Concurso)
            .HasForeignKey(x => x.ConcursoId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Premios)
            .WithOne(x => x.Concurso)
            .HasForeignKey(x => x.ConcursoId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.GanhadoresPorLocal)
            .WithOne(x => x.Concurso)
            .HasForeignKey(x => x.ConcursoId)
            .OnDelete(DeleteBehavior.Cascade);
        
        b.HasOne(x => x.Lotofacil)
            .WithOne(x => x.Concurso)
            .HasForeignKey<LotofacilDetalhe>(x => x.ConcursoId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.MegaSena)
            .WithOne(x => x.Concurso)
            .HasForeignKey<MegaSenaDetalhe>(x => x.ConcursoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}