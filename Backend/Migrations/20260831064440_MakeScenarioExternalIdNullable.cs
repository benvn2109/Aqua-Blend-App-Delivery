using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AquaBlend.Api.Migrations
{
    /// <inheritdoc />
    public partial class MakeScenarioExternalIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder){
    migrationBuilder.AlterColumn<string>(
        name: "ExternalId",
        table: "Scenarios",
        type: "text",
        nullable: true,
        oldClrType: typeof(string),
        oldType: "text",
        oldDefaultValue: "");

    migrationBuilder.Sql(
        "UPDATE \"Scenarios\" SET \"ExternalId\" = NULL WHERE \"ExternalId\" = '';");
}

        /// <inheritdoc />
       protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(
        "UPDATE \"Scenarios\" SET \"ExternalId\" = '' WHERE \"ExternalId\" IS NULL;");

    migrationBuilder.AlterColumn<string>(
        name: "ExternalId",
        table: "Scenarios",
        type: "text",
        nullable: false,
        defaultValue: "",
        oldClrType: typeof(string),
        oldType: "text",
        oldNullable: true);
}
    }
}
