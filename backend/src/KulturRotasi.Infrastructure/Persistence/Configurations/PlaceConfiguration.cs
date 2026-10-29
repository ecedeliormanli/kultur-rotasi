using KulturRotasi.Domain.Places;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KulturRotasi.Infrastructure.Persistence.Configurations;

internal sealed class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.ToTable("Places");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Slug).HasMaxLength(Place.SlugMaxLength).IsRequired();
        builder.HasIndex(p => p.Slug).IsUnique();

        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);

        // geography = round-earth coordinates; distances come back in metres
        builder.Property(p => p.Location).HasColumnType("geography").IsRequired();

        builder.HasOne(p => p.Province)
            .WithMany()
            .HasForeignKey(p => p.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Read from Location, not stored in their own columns
        builder.Ignore(p => p.Latitude);
        builder.Ignore(p => p.Longitude);

        builder.OwnsMany(p => p.Translations, translation =>
        {
            translation.ToTable("PlaceTranslations");
            translation.WithOwner().HasForeignKey(t => t.PlaceId);
            translation.HasKey(t => new { t.PlaceId, t.LanguageCode });
            translation.Property(t => t.LanguageCode).HasMaxLength(2).IsUnicode(false);
            translation.Property(t => t.Name).HasMaxLength(PlaceTranslation.NameMaxLength).IsRequired();
            translation.Property(t => t.Description).HasMaxLength(PlaceTranslation.DescriptionMaxLength);
        });

        builder.Navigation(p => p.Translations).HasField("_translations");
    }
}
