using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCharacterProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_experience",
                table: "characters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_level",
                table: "characters");

            migrationBuilder.RenameColumn(
                name: "Experience",
                table: "characters",
                newName: "TotalXp");

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "Reset",
                table: "characters",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // O Level antigo começava em 1 e não possuía regra de ganho de XP.
            // Reconstrói o novo Level/Reset a partir do XP histórico preservado.
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    character_row record;
                    remaining_xp bigint;
                    required_xp numeric;
                    global_level integer;
                BEGIN
                    FOR character_row IN SELECT "Id", "TotalXp" FROM characters LOOP
                        remaining_xp := character_row."TotalXp";
                        global_level := 0;
                        LOOP
                            required_xp := ceil(100.0 * power(1.01::double precision, global_level));
                            EXIT WHEN required_xp >= 9223372036854775807 OR remaining_xp < required_xp;
                            remaining_xp := remaining_xp - required_xp::bigint;
                            global_level := global_level + 1;
                        END LOOP;

                        UPDATE characters
                        SET "Level" = global_level % 200,
                            "Reset" = global_level / 200
                        WHERE "Id" = character_row."Id";
                    END LOOP;
                END $$;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_level",
                table: "characters",
                sql: "\"Level\" >= 0 AND \"Level\" < 200");

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_reset",
                table: "characters",
                sql: "\"Reset\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_total_xp",
                table: "characters",
                sql: "\"TotalXp\" >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_level",
                table: "characters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_reset",
                table: "characters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_total_xp",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "Reset",
                table: "characters");

            migrationBuilder.RenameColumn(
                name: "TotalXp",
                table: "characters",
                newName: "Experience");

            migrationBuilder.AlterColumn<int>(
                name: "Level",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE characters SET "Level" = "Level" + 1;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_experience",
                table: "characters",
                sql: "\"Experience\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_level",
                table: "characters",
                sql: "\"Level\" >= 1");
        }
    }
}
