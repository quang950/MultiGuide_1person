using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Configurations;

public class PlayLogConfiguration : IEntityTypeConfiguration<PlayLog>
{
    public void Configure(EntityTypeBuilder<PlayLog> builder)
    {
        builder.ToTable("play_log");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.PassId)
            .HasColumnName("pass_id")
            .IsRequired();

        builder.Property(x => x.PoiId)
            .HasColumnName("poi_id")
            .IsRequired();

        builder.Property(x => x.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(x => x.ListenedSec)
            .HasColumnName("listened_sec")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.Completed)
            .HasColumnName("completed")
            .HasDefaultValue(false)
            .IsRequired();

        builder.HasIndex(x => new { x.PoiId, x.StartedAt })
            .HasDatabaseName("idx_playlog_poi_started");

        builder.HasOne(x => x.Pass)
            .WithMany(p => p.PlayLogs)
            .HasForeignKey(x => x.PassId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Poi)
            .WithMany(p => p.PlayLogs)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Language)
            .WithMany(l => l.PlayLogs)
            .HasForeignKey(x => x.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
