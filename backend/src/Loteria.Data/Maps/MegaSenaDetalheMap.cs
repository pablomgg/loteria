using Loteria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loteria.Data.Maps;

public class MegaSenaDetalheMap : IEntityTypeConfiguration<MegaSenaDetalhe>
{
    public void Configure(EntityTypeBuilder<MegaSenaDetalhe> b)
    {
        b.ToTable("MegaSenaDetalhe", "dbo");
        
        b.HasKey(x => x.ConcursoId);

        b.Property(x => x.Acumulado)
            .IsRequired();
        
        b.Property(x => x.IndicadorConcursoEspecial)
            .IsRequired();

        b.Property(x => x.Observacao)
            .HasMaxLength(300);
        
        b.Property(x => x.ValorArrecadado)
            .HasColumnType("decimal(18,2)");
        
        b.Property(x => x.ValorAcumulado)
            .HasColumnType("decimal(18,2)");
    }
}