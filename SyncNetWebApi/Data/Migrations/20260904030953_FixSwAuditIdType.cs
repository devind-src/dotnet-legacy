using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncNetApi.Data.Migrations
{
    /// <inheritdoc />
    // NOTE: hand-trimmed to a no-op. The auto-generated version tried to (a) DropColumn
    // flag_add/flag_edit/flag_delete on dashboard_role_menu — columns that were declared on
    // the old C# entity but never actually existed on the real table (see
    // DashboardRoleMenu.cs), which would fail with "column does not exist"; and (b)
    // AlterColumn nullable=true + type="text" on a dozen dashboard_*/sw_audit columns that
    // are ALREADY nullable in the real DB (confirmed by direct inspection) and are typed
    // `character varying`, not `text` — those AlterColumns exist only because the EF model
    // snapshot's PREVIOUS state (from before this session's entity fixes) never matched the
    // real schema in the first place (these tables predate our migrations and were never
    // CREATEd by us). None of that needs to touch the real database — this migration exists
    // solely so `dotnet ef migrations add` going forward diffs against an accurate snapshot.
    public partial class FixSwAuditIdType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
