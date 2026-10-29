using KulturRotasi.Domain.Provinces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KulturRotasi.Infrastructure.Persistence.Configurations;

internal sealed class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("Provinces");
        builder.HasKey(p => p.Id);

        // The id is the plate code (1–81), so the database must not generate it
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name).HasMaxLength(Province.NameMaxLength).IsRequired();
        builder.Property(p => p.Boundary).HasColumnType("geography").IsRequired();
    }
}
