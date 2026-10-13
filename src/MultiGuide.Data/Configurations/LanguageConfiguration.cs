using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiGuide.Domain.Entities;

namespace MultiGuide.Data.Configurations;

public class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("language");

        builder.HasKey(x => x.Code);

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasMany(x => x.Translations)
            .WithOne(t => t.Language)
            .HasForeignKey(t => t.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.PlayLogs)
            .WithOne(p => p.Language)
            .HasForeignKey(p => p.LanguageCode)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
