using Loteria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loteria.Data.Maps;

public class ConcursoGanhadorLocalMap : IEntityTypeConfiguration<ConcursoGanhadorLocal>
{
    public void Configure(EntityTypeBuilder<ConcursoGanhadorLocal> b)
    {
        b.ToTable("ConcursoGanhadorLocal", "dbo");
        
        b.HasKey(x => x.Id);

        b.Property(x => x.ConcursoId)
            .IsRequired();

        b.Property(x => x.Quantidade)
            .IsRequired();

        b.Property(x => x.Municipio)
            .HasMaxLength(120);

        b.Property(x => x.Uf)
            .HasMaxLength(2)
            .IsUnicode(false)
            .IsRequired();

        b.HasIndex(x => x.ConcursoId)
            .HasDatabaseName("IX_ConcursoGanhadorLocal_ConcursoId");
    }
}