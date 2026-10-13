using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Configurations;

public class PoiTranslationConfiguration : IEntityTypeConfiguration<PoiTranslation>
{
    public void Configure(EntityTypeBuilder<PoiTranslation> builder)
    {
        builder.ToTable("poi_translation");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.PoiId)
            .HasColumnName("poi_id")
            .IsRequired();

        builder.Property(x => x.LanguageCode)
            .HasColumnName("language_code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.AudioPath)
            .HasColumnName("audio_path")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(x => x.AudioDurationSec)
            .HasColumnName("audio_duration_sec")
            .IsRequired(false);

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.HasIndex(x => new { x.PoiId, x.LanguageCode })
            .IsUnique()
            .HasDatabaseName("uq_poi_translation_poi_lang");

        builder.HasOne(x => x.Poi)
            .WithMany(p => p.Translations)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Language)
            .WithMany(l => l.Translations)
            .HasForeignKey(x => x.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
