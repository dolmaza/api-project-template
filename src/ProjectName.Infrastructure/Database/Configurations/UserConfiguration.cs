using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProjectName.Domain.AggregatesModel.IdentityAggregate;

namespace ProjectName.Infrastructure.Database.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(500);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ModifiedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");
    }
}
