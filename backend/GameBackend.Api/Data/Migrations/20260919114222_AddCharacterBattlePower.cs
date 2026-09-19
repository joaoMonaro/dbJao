using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterBattlePower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BaseBattlePower",
                table: "characters",
                type: "bigint",
                nullable: false,
                defaultValue: 10L);

            migrationBuilder.Sql(
                """
                UPDATE characters
                SET "BaseBattlePower" = LEAST(
                    9223372036854775807::numeric,
                    10::numeric + (("Reset"::numeric * 200) + "Level"::numeric) * 100
                )::bigint;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_base_battle_power",
                table: "characters",
                sql: "\"BaseBattlePower\" >= 10");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_base_battle_power",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "BaseBattlePower",
                table: "characters");
        }
    }
}
