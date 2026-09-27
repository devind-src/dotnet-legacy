using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_product_bins" — Configuration &gt; Base &gt; BIN, new in Phase 5b.
    /// This is NOT the same table as PosBase's "sw_posnet_bin" (Phase 4, already done) nor the
    /// unused legacy "sw_bins"/SwBin.cs — see HANDOFF_PHASE5_CONFIGURATION.md §4. id (bigint)
    /// was manually computed as max(id)+1 (legacy quirk); converted to a real Postgres
    /// IDENTITY column — DB now assigns it on insert. No status/audit columns; 0 rows currently.</summary>
    [Table("sw_product_bins", Schema = "public")]
    public class SwProductBin
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? bin { get; set; }
        public string? cbc { get; set; }
        public string? bank_name { get; set; }
    }
}
