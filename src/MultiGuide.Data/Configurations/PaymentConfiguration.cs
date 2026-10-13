using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiGuide.Domain.Entities;
using MultiGuide.Domain.Enums;

namespace MultiGuide.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AreaId)
            .HasColumnName("area_id")
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasColumnName("currency")
            .HasMaxLength(10)
            .HasDefaultValue("USD")
            .IsRequired();

        builder.Property(x => x.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .HasDefaultValue("mock")
            .IsRequired();

        builder.Property(x => x.ProviderRef)
            .HasColumnName("provider_ref")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("uq_payment_idempotency_key");

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion<string>()
            .HasDefaultValue(PaymentStatus.Pending)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasOne(x => x.Area)
            .WithMany(a => a.Payments)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
