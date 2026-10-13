using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;

namespace MultiGuide.Data.Configurations;

public class AccessPassConfiguration : IEntityTypeConfiguration<AccessPass>
{
    public void Configure(EntityTypeBuilder<AccessPass> builder)
    {
        builder.ToTable("access_pass");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AreaId)
            .HasColumnName("area_id")
            .IsRequired();

        builder.Property(x => x.PaymentId)
            .HasColumnName("payment_id")
            .IsRequired();

        builder.HasIndex(x => x.PaymentId)
            .IsUnique()
            .HasDatabaseName("uq_access_pass_payment_id");

        builder.Property(x => x.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("uq_access_pass_token_hash");

        builder.Property(x => x.IssuedAt)
            .HasColumnName("issued_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(x => x.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(AccessPassStatus.Active)
            .IsRequired();

        builder.HasOne(x => x.Area)
            .WithMany(a => a.AccessPasses)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Payment)
            .WithOne(p => p.AccessPass)
            .HasForeignKey<AccessPass>(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.PlayLogs)
            .WithOne(p => p.Pass)
            .HasForeignKey(p => p.PassId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
