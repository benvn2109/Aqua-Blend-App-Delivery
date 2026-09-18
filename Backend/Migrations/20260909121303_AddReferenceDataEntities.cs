using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AquaBlend.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceDataEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActivationCost",
                table: "WaterSources",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityOrigin",
                table: "WaterSources",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AvailabilityStatus",
                table: "WaterSources",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CostPerMl",
                table: "WaterSources",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "WaterSources",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasEstimatedValues",
                table: "WaterSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsModelReady",
                table: "WaterSources",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxWithdrawalMlPerDay",
                table: "WaterSources",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinWithdrawalMlPerDay",
                table: "WaterSources",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "QualityAlkalinity",
                table: "WaterSources",
                type: "numeric",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DemandZones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DemandMlPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    MaximumProcessingCapacityMlPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    TreatmentCostPerMl = table.Column<decimal>(type: "numeric", nullable: false),
                    ActivationCost = table.Column<decimal>(type: "numeric", nullable: true),
                    IsModelReady = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QualityProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Parameter = table.Column<string>(type: "text", nullable: false),
                    Unit = table.Column<string>(type: "text", nullable: false),
                    ConstraintMin = table.Column<decimal>(type: "numeric", nullable: false),
                    ConstraintMax = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlantZoneLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    DemandZoneId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaximumCapacityMlPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlantZoneLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlantZoneLinks_DemandZones_DemandZoneId",
                        column: x => x.DemandZoneId,
                        principalTable: "DemandZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlantZoneLinks_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SourcePlantLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WaterSourceId = table.Column<int>(type: "integer", nullable: false),
                    PlantId = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaximumCapacityMlPerDay = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SourcePlantLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SourcePlantLinks_Plants_PlantId",
                        column: x => x.PlantId,
                        principalTable: "Plants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SourcePlantLinks_WaterSources_WaterSourceId",
                        column: x => x.WaterSourceId,
                        principalTable: "WaterSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaterSources_ExternalId",
                table: "WaterSources",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandZones_ExternalId",
                table: "DemandZones",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plants_ExternalId",
                table: "Plants",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlantZoneLinks_DemandZoneId",
                table: "PlantZoneLinks",
                column: "DemandZoneId");

            migrationBuilder.CreateIndex(
                name: "IX_PlantZoneLinks_PlantId",
                table: "PlantZoneLinks",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePlantLinks_PlantId",
                table: "SourcePlantLinks",
                column: "PlantId");

            migrationBuilder.CreateIndex(
                name: "IX_SourcePlantLinks_WaterSourceId",
                table: "SourcePlantLinks",
                column: "WaterSourceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlantZoneLinks");

            migrationBuilder.DropTable(
                name: "QualityProfiles");

            migrationBuilder.DropTable(
                name: "SourcePlantLinks");

            migrationBuilder.DropTable(
                name: "DemandZones");

            migrationBuilder.DropTable(
                name: "Plants");

            migrationBuilder.DropIndex(
                name: "IX_WaterSources_ExternalId",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "ActivationCost",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "AvailabilityOrigin",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "AvailabilityStatus",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "CostPerMl",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "HasEstimatedValues",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "IsModelReady",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "MaxWithdrawalMlPerDay",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "MinWithdrawalMlPerDay",
                table: "WaterSources");

            migrationBuilder.DropColumn(
                name: "QualityAlkalinity",
                table: "WaterSources");
        }
    }
}
