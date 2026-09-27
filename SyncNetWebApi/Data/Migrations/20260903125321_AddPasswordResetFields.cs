using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncNetApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        // NOTE: hand-trimmed. The auto-generated migration also included an AddColumn for
        // dashboard_user.must_change_password — that column was added directly to the
        // production DB before this migration existed, so re-adding it here would fail with
        // "column already exists". Only dashboard_user_reset's new columns (this feature's
        // own table) and the two indexes are this migration's job. The real uniqueness
        // guarantee on dashboard_user.email is a case-insensitive PARTIAL unique index
        // (raw SQL — EF's cross-provider model has no clean way to express `lower(email)` as
        // an index key); IX_dashboard_user_email above it is just a plain lookup index.
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "consumed_at",
                schema: "public",
                table: "dashboard_user_reset",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "requested_ip",
                schema: "public",
                table: "dashboard_user_reset",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_name",
                schema: "public",
                table: "dashboard_user_reset",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_user_reset_user_name",
                schema: "public",
                table: "dashboard_user_reset",
                column: "user_name");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_user_reset_vcode",
                schema: "public",
                table: "dashboard_user_reset",
                column: "vcode");

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_user_email",
                schema: "public",
                table: "dashboard_user",
                column: "email");

            // Verified against production data before writing this: 0 duplicate emails
            // (case-insensitive) and 0 null/blank emails among existing dashboard_user rows,
            // so this is safe to apply as-is with no cleanup step needed first.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS ux_dashboard_user_email " +
                "ON public.dashboard_user (lower(email)) " +
                "WHERE email IS NOT NULL AND email <> '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS public.ux_dashboard_user_email;");

            migrationBuilder.DropIndex(
                name: "IX_dashboard_user_reset_user_name",
                schema: "public",
                table: "dashboard_user_reset");

            migrationBuilder.DropIndex(
                name: "IX_dashboard_user_reset_vcode",
                schema: "public",
                table: "dashboard_user_reset");

            migrationBuilder.DropIndex(
                name: "IX_dashboard_user_email",
                schema: "public",
                table: "dashboard_user");

            migrationBuilder.DropColumn(
                name: "consumed_at",
                schema: "public",
                table: "dashboard_user_reset");

            migrationBuilder.DropColumn(
                name: "requested_ip",
                schema: "public",
                table: "dashboard_user_reset");

            migrationBuilder.DropColumn(
                name: "user_name",
                schema: "public",
                table: "dashboard_user_reset");
        }
    }
}
