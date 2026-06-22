using Loteria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loteria.Data.Maps;

public class LotofacilDetalheMap : IEntityTypeConfiguration<LotofacilDetalhe>
{
    public void Configure(EntityTypeBuilder<LotofacilDetalhe> b)
    {
        b.ToTable("LotofacilDetalhe", "dbo");
        
        b.HasKey(x => x.ConcursoId);

        b.Property(x => x.Acumulado)
            .IsRequired();

        b.Property(x => x.IndicadorConcursoEspecial)
            .IsRequired();

        b.Property(x => x.Observacao)
            .HasMaxLength(300);

        b.Property(x => x.ValorArrecadado)
            .HasColumnType("decimal(18,2)");
    }
}