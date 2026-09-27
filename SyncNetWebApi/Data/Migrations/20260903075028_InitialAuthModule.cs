using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SyncNetApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuthModule : Migration
    {
        /// <inheritdoc />
        // NOTE: hand-trimmed. `dotnet ef migrations add` originally generated CreateTable
        // calls for every entity in SyncNetDbContext, including dashboard_menu,
        // dashboard_role, dashboard_role_menu, dashboard_user, dashboard_user_log,
        // dashboard_user_reset and sw_audit — those already exist in the production
        // `syncnet` database (owned by the legacy Blazor app) and must NOT be
        // (re)created here, or `dotnet ef database update` fails with
        // "relation already exists". This migration is a baseline: it only creates the
        // two tables this API introduces. The model snapshot below is left untouched —
        // it must still reflect every entity for future migrations to diff correctly.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "api_refresh_token",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    TokenHash = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "text", nullable: true),
                    CreatedByIp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_refresh_token", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "api_token_blacklist",
                schema: "public",
                columns: table => new
                {
                    Jti = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_api_token_blacklist", x => x.Jti);
                });

            migrationBuilder.CreateIndex(
                name: "IX_api_refresh_token_TokenHash",
                schema: "public",
                table: "api_refresh_token",
                column: "TokenHash");

            migrationBuilder.CreateIndex(
                name: "IX_api_refresh_token_UserName",
                schema: "public",
                table: "api_refresh_token",
                column: "UserName");

            migrationBuilder.CreateIndex(
                name: "IX_api_token_blacklist_ExpiresAt",
                schema: "public",
                table: "api_token_blacklist",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "api_refresh_token",
                schema: "public");

            migrationBuilder.DropTable(
                name: "api_token_blacklist",
                schema: "public");
        }
    }
}
