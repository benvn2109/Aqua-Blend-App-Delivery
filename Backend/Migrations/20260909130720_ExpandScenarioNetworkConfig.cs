using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaBlend.Api.Migrations
{
    /// <inheritdoc />
    public partial class ExpandScenarioNetworkConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OptimisationRuns_ScenarioId_CreatedAt",
                table: "OptimisationRuns");

            migrationBuilder.AddColumn<bool>(
                name: "IsReady",
                table: "Scenarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NetworkConfigJson",
                table: "Scenarios",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "ValidationIssuesJson",
                table: "Scenarios",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateIndex(
                name: "IX_OptimisationRuns_ScenarioId_CreatedAt",
                table: "OptimisationRuns",
                columns: new[] { "ScenarioId", "CreatedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OptimisationRuns_ScenarioId_CreatedAt",
                table: "OptimisationRuns");

            migrationBuilder.DropColumn(
                name: "IsReady",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "NetworkConfigJson",
                table: "Scenarios");

            migrationBuilder.DropColumn(
                name: "ValidationIssuesJson",
                table: "Scenarios");

            migrationBuilder.CreateIndex(
                name: "IX_OptimisationRuns_ScenarioId_CreatedAt",
                table: "OptimisationRuns",
                columns: new[] { "ScenarioId", "CreatedAt" });
        }
    }
}
