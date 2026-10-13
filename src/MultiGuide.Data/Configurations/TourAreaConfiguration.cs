using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Configurations;

public class TourAreaConfiguration : IEntityTypeConfiguration<TourArea>
{
    public void Configure(EntityTypeBuilder<TourArea> builder)
    {
        builder.ToTable("tour_area");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("uq_tour_area_code");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CenterLat)
            .HasColumnName("center_lat")
            .IsRequired();

        builder.Property(x => x.CenterLng)
            .HasColumnName("center_lng")
            .IsRequired();

        builder.Property(x => x.RadiusM)
            .HasColumnName("radius_m")
            .IsRequired();

        builder.Property(x => x.PriceUsd)
            .HasColumnName("price_usd")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(x => x.PassTtlHours)
            .HasColumnName("pass_ttl_hours")
            .HasDefaultValue(24)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasMany(x => x.Pois)
            .WithOne(p => p.Area)
            .HasForeignKey(p => p.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Payments)
            .WithOne(p => p.Area)
            .HasForeignKey(p => p.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.AccessPasses)
            .WithOne(p => p.Area)
            .HasForeignKey(p => p.AreaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
