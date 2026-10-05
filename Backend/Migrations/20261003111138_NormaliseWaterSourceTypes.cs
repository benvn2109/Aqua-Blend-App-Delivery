using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaBlend.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormaliseWaterSourceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Normalise WaterSources.Type to the SourceTypes vocabulary
            // (reservoir / river / groundwater). Matching is case-insensitive.
            // Legacy "Surface" maps to reservoir. Values outside the agreed
            // mapping are left untouched.
            migrationBuilder.Sql(
                """
                UPDATE "WaterSources" SET "Type" = 'reservoir'
                WHERE LOWER("Type") IN ('surface', 'reservoir') AND "Type" <> 'reservoir';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "WaterSources" SET "Type" = 'river'
                WHERE LOWER("Type") = 'river' AND "Type" <> 'river';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "WaterSources" SET "Type" = 'groundwater'
                WHERE LOWER("Type") = 'groundwater' AND "Type" <> 'groundwater';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // APPROXIMATE REVERSE - not lossless. Up() collapsed "Surface" and
            // any casing of reservoir/river into lowercase values, so the original
            // value can't be recovered. reservoir and river both map back to the
            // legacy "Surface"; groundwater maps back to "Groundwater". A row that
            // was originally e.g. "Reservoir" or "River" will come back as "Surface".
            migrationBuilder.Sql(
                """
                UPDATE "WaterSources" SET "Type" = 'Surface'
                WHERE "Type" IN ('reservoir', 'river');
                """);

            migrationBuilder.Sql(
                """
                UPDATE "WaterSources" SET "Type" = 'Groundwater'
                WHERE "Type" = 'groundwater';
                """);
        }
    }
}
