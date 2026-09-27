using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_cities" — Configuration &gt; Base &gt; City. Upgraded from Phase 4's
    /// read-only lookup to full CRUD in Phase 5b. id is bigint GENERATED ALWAYS AS IDENTITY
    /// (verified live via information_schema) — never set it explicitly, let Postgres generate.
    /// No status/audit columns on this table (verified live — genuinely just these 4 columns).
    /// Still backs the read-only "City" dropdown on PosBase &gt; SoundBox via LookupsController.</summary>
    [Table("sw_cities", Schema = "public")]
    public class SwCity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? city { get; set; }
        public string? province { get; set; }
        public string? city_type { get; set; }
    }
}
