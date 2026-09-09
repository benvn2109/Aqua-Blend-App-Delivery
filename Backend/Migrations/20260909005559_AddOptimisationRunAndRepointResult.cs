using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AquaBlend.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOptimisationRunAndRepointResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ScenarioId",
                table: "OptimisationResults",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "RunId",
                table: "OptimisationResults",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OptimisationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ScenarioId = table.Column<int>(type: "integer", nullable: false),
                    WorkflowStatus = table.Column<string>(type: "text", nullable: false),
                    SolverStatus = table.Column<string>(type: "text", nullable: true),
                    ScenarioSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptimisationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptimisationRuns_Scenarios_ScenarioId",
                        column: x => x.ScenarioId,
                        principalTable: "Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    r RECORD;
                    new_run_id integer;
                BEGIN
                    FOR r IN SELECT ""Id"", ""ScenarioId"", ""Status"", ""CreatedAt""
                             FROM ""OptimisationResults""
                             WHERE ""RunId"" IS NULL
                    LOOP
                        INSERT INTO ""OptimisationRuns""
                            (""ScenarioId"", ""WorkflowStatus"", ""SolverStatus"", ""ScenarioSnapshotJson"", ""CreatedAt"")
                        VALUES
                            (r.""ScenarioId"", 'completed', r.""Status"", '{}', r.""CreatedAt"")
                        RETURNING ""Id"" INTO new_run_id;

                        UPDATE ""OptimisationResults""
                        SET ""RunId"" = new_run_id
                        WHERE ""Id"" = r.""Id"";
                    END LOOP;
                END $$;
            ");

            migrationBuilder.AlterColumn<int>(
                name: "RunId",
                table: "OptimisationResults",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OptimisationResults_RunId",
                table: "OptimisationResults",
                column: "RunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OptimisationRuns_ScenarioId_CreatedAt",
                table: "OptimisationRuns",
                columns: new[] { "ScenarioId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_OptimisationResults_OptimisationRuns_RunId",
                table: "OptimisationResults",
                column: "RunId",
                principalTable: "OptimisationRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OptimisationResults_OptimisationRuns_RunId",
                table: "OptimisationResults");

            migrationBuilder.DropTable(
                name: "OptimisationRuns");

            migrationBuilder.DropIndex(
                name: "IX_OptimisationResults_RunId",
                table: "OptimisationResults");

            migrationBuilder.DropColumn(
                name: "RunId",
                table: "OptimisationResults");

            migrationBuilder.AlterColumn<int>(
                name: "ScenarioId",
                table: "OptimisationResults",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
