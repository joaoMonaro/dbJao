using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameBackend.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBlocked",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxHealth",
                table: "characters",
                type: "integer",
                nullable: false,
                defaultValue: 100);

            migrationBuilder.CreateTable(
                name: "game_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenHash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsConsumed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    GameServerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_game_sessions_characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                UPDATE characters
                SET "MaxHealth" = GREATEST(100, "CurrentHealth")
                """);

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_health_range",
                table: "characters",
                sql: "\"CurrentHealth\" <= \"MaxHealth\"");

            migrationBuilder.AddCheckConstraint(
                name: "ck_characters_max_health",
                table: "characters",
                sql: "\"MaxHealth\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_CharacterId_IsConsumed_ExpiresAt",
                table: "game_sessions",
                columns: new[] { "CharacterId", "IsConsumed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_TokenHash",
                table: "game_sessions",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_sessions_UserId_IsConsumed_ExpiresAt",
                table: "game_sessions",
                columns: new[] { "UserId", "IsConsumed", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_sessions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_health_range",
                table: "characters");

            migrationBuilder.DropCheckConstraint(
                name: "ck_characters_max_health",
                table: "characters");

            migrationBuilder.DropColumn(
                name: "IsBlocked",
                table: "users");

            migrationBuilder.DropColumn(
                name: "MaxHealth",
                table: "characters");
        }
    }
}
