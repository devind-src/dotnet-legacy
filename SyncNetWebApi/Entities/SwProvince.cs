using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_province" — Configuration &gt; Base &gt; Province, new in Phase 5b.
    /// id is bigint GENERATED ALWAYS AS IDENTITY (verified live via information_schema) — same
    /// as City, never set it explicitly. No status/audit columns on this table.</summary>
    [Table("sw_province", Schema = "public")]
    public class SwProvince
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long id { get; set; }
        public string? province { get; set; }
        public string? capital { get; set; }
        public string? island { get; set; }
    }
}
