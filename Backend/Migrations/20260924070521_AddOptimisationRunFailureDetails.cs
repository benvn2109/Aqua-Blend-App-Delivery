using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaBlend.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimisationRunFailureDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                table: "OptimisationRuns",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FailureSource",
                table: "OptimisationRuns",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailureReason",
                table: "OptimisationRuns");

            migrationBuilder.DropColumn(
                name: "FailureSource",
                table: "OptimisationRuns");
        }
    }
}
