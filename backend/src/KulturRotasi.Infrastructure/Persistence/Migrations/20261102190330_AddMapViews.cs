using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KulturRotasi.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Read-only views for GeoServer (WMS/WFS). They flatten the translations into columns and expose the
    /// shapes as planar <c>geometry</c>, which GeoServer renders more reliably than <c>geography</c>.
    /// </summary>
    public partial class AddMapViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE VIEW dbo.MapPlaces AS
                SELECT p.Id,
                       p.Slug,
                       p.Type,
                       p.ProvinceId,
                       p.IsUnescoWorldHeritage,
                       tr.Name AS NameTr,
                       en.Name AS NameEn,
                       geometry::STGeomFromWKB(p.Location.STAsBinary(), 4326) AS Geom
                FROM dbo.Places p
                LEFT JOIN dbo.PlaceTranslations tr ON tr.PlaceId = p.Id AND tr.LanguageCode = 'tr'
                LEFT JOIN dbo.PlaceTranslations en ON en.PlaceId = p.Id AND en.LanguageCode = 'en';
                """);

            migrationBuilder.Sql("""
                CREATE VIEW dbo.MapProvinces AS
                SELECT pr.Id AS PlateCode,
                       pr.Name,
                       (SELECT COUNT(*) FROM dbo.Places p WHERE pr.Boundary.STContains(p.Location) = 1) AS PlaceCount,
                       geometry::STGeomFromWKB(pr.Boundary.STAsBinary(), 4326) AS Geom
                FROM dbo.Provinces pr;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.MapProvinces;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS dbo.MapPlaces;");
        }
    }
}
