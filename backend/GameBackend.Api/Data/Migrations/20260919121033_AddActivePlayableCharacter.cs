using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivePlayableCharacter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActiveCharacterId",
                table: "characters",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "goku");

            // O default não nulo também preenche os registros criados antes desta migration.

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_active_character_id",
                table: "characters",
                sql: "length(btrim(\"ActiveCharacterId\")) > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_active_character_id",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "ActiveCharacterId",
                table: "characters");
        }
    }
}
