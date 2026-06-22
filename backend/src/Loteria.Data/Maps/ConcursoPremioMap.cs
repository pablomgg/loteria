using Loteria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loteria.Data.Maps;

public class ConcursoPremioMap : IEntityTypeConfiguration<ConcursoPremio>
{
    public void Configure(EntityTypeBuilder<ConcursoPremio> b)
    {
        b.ToTable("ConcursoPremio", "dbo");
        
        b.HasKey(x => x.Id);

        b.Property(x => x.ConcursoId)
            .IsRequired();

        b.Property(x => x.Faixa)
            .IsRequired();

        b.Property(x => x.DescricaoFaixa)
            .HasMaxLength(80);

        b.Property(x => x.NumeroDeGanhadores)
            .IsRequired();

        b.Property(x => x.ValorPremio)
            .HasColumnType("decimal(18,2)")
            .IsRequired();
        
        b.HasIndex(x => new { x.ConcursoId, x.Faixa })
            .IsUnique()
            .HasDatabaseName("UX_ConcursoPremio_Concurso_Faixa");

        b.HasIndex(x => x.ConcursoId)
            .HasDatabaseName("IX_ConcursoPremio_ConcursoId");
    }
}