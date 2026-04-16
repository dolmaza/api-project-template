using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Infrastructure.Idempotency;

namespace ProjectName.Infrastructure.Database.Configurations;

public class ClientRequestConfiguration : IEntityTypeConfiguration<ClientRequest>
{
    public void Configure(EntityTypeBuilder<ClientRequest> builder)
    {
        builder.ToTable("Requests", ApplicationDbContext.PublicSchema);
        
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Url)
            .IsRequired()
            .HasMaxLength(2048);
        
        builder.Property(e => e.Time)
            .IsRequired();
        
        builder.Property(e => e.FinishedAt)
            .IsRequired(false);
        
        builder.Property(e => e.Duration)
            .IsRequired(false);
        
        builder.HasIndex(e => e.FinishedAt)
            .HasDatabaseName("IX_Requests_FinishedAt");
        
        builder.HasIndex(e => new { e.Id, e.FinishedAt })
            .HasDatabaseName("IX_Requests_Id_FinishedAt");
    }
}