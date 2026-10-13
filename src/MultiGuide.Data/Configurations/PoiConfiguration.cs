using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Configurations;

public class PoiConfiguration : IEntityTypeConfiguration<Poi>
{
    public void Configure(EntityTypeBuilder<Poi> builder)
    {
        builder.ToTable("poi");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.AreaId)
            .HasColumnName("area_id")
            .IsRequired();

        builder.Property(x => x.Lat)
            .HasColumnName("lat")
            .IsRequired();

        builder.Property(x => x.Lng)
            .HasColumnName("lng")
            .IsRequired();

        builder.Property(x => x.TriggerRadiusM)
            .HasColumnName("trigger_radius_m")
            .HasDefaultValue(30.0)
            .IsRequired();

        builder.Property(x => x.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(x => new { x.AreaId, x.IsActive })
            .HasDatabaseName("idx_poi_area_active");

        builder.HasIndex(x => new { x.Lat, x.Lng })
            .HasDatabaseName("idx_poi_location");

        builder.HasOne(x => x.Area)
            .WithMany(a => a.Pois)
            .HasForeignKey(x => x.AreaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Translations)
            .WithOne(t => t.Poi)
            .HasForeignKey(t => t.PoiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.PlayLogs)
            .WithOne(p => p.Poi)
            .HasForeignKey(p => p.PoiId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
