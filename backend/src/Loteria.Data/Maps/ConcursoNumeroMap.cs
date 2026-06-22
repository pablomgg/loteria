using Loteria.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Loteria.Data.Maps;

public class ConcursoNumeroMap : IEntityTypeConfiguration<ConcursoNumero>
{
    public void Configure(EntityTypeBuilder<ConcursoNumero> b)
    {
        b.ToTable("ConcursoNumero", "dbo");
        
        b.HasKey(x => x.Id);

        b.Property(x => x.ConcursoId)
            .IsRequired();
        
        b.Property(x => x.Numero)
            .HasColumnType("smallint")
            .IsRequired();

        b.Property(x => x.Posicao)
            .HasColumnType("tinyint");
        
        b.HasIndex(x => new { x.ConcursoId, x.Numero })
            .IsUnique()
            .HasDatabaseName("UX_ConcursoNumero_Concurso_Numero");
        
        b.HasIndex(x => new { x.ConcursoId, x.Posicao })
            .IsUnique()
            .HasFilter("[Posicao] IS NOT NULL")
            .HasDatabaseName("UX_ConcursoNumero_Concurso_Posicao");

        b.HasIndex(x => x.ConcursoId)
            .HasDatabaseName("IX_ConcursoNumero_ConcursoId");
    }
}