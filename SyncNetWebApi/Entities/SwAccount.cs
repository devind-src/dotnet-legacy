using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_accounts" — Card &gt; Base &gt; Account. id was manually computed as
    /// max(id)+1 (legacy quirk); converted to a real Postgres IDENTITY column — DB now assigns
    /// it on insert. group_id is a real FK (ON DELETE NO ACTION) to sw_group.group_id, but nullable in the
    /// real column even though the legacy form treats it as mandatory (IsValid()). acc_type is
    /// char(2), matched against sw_account_types.acct_type (Configuration &gt; Base &gt; Account
    /// Type — reused as the dropdown source here, no new lookup needed). pan_verification/
    /// pin_verification are varchar(1) "0"/"1" flags. The legacy list's "Acc Name" column is a
    /// JOIN-time value from sw_account_types.name, not a physical column here — resolved in
    /// the service layer, same pattern as Product &gt; Merchant's Category. Real table also has
    /// status/created_by/created_dt/updated_by/updated_dt columns, never mapped or written by
    /// the legacy model/form — dead columns, not mapped here.</summary>
    [Table("sw_accounts", Schema = "public")]
    public class SwAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public int? group_id { get; set; }
        [MaxLength(2)]
        public string? acc_type { get; set; }
        [MaxLength(1)]
        public string? pan_verification { get; set; }
        [MaxLength(1)]
        public string? pin_verification { get; set; }
    }
}
