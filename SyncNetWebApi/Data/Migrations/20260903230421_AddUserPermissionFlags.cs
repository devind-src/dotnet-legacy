using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncNetApi.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPermissionFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allow_add",
                schema: "public",
                table: "dashboard_user",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_delete",
                schema: "public",
                table: "dashboard_user",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "allow_edit",
                schema: "public",
                table: "dashboard_user",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "allow_add",
                schema: "public",
                table: "dashboard_user");

            migrationBuilder.DropColumn(
                name: "allow_delete",
                schema: "public",
                table: "dashboard_user");

            migrationBuilder.DropColumn(
                name: "allow_edit",
                schema: "public",
                table: "dashboard_user");
        }
    }
}
