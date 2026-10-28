using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace KulturRotasi.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Boundary = table.Column<Geometry>(type: "geography", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Location = table.Column<Point>(type: "geography", nullable: false),
                    ProvinceId = table.Column<int>(type: "int", nullable: false),
                    IsUnescoWorldHeritage = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Places_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaceTranslations",
                columns: table => new
                {
                    PlaceId = table.Column<int>(type: "int", nullable: false),
                    LanguageCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceTranslations", x => new { x.PlaceId, x.LanguageCode });
                    table.ForeignKey(
                        name: "FK_PlaceTranslations_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Places_ProvinceId",
                table: "Places",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Slug",
                table: "Places",
                column: "Slug",
                unique: true);

            // EF Core cannot model spatial indexes, so they are created with plain SQL.
            // They let SQL Server answer "what is near / inside this shape" without scanning every row.
            migrationBuilder.Sql("CREATE SPATIAL INDEX SIX_Places_Location ON Places(Location);");
            migrationBuilder.Sql("CREATE SPATIAL INDEX SIX_Provinces_Boundary ON Provinces(Boundary);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaceTranslations");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "Provinces");
        }
    }
}
