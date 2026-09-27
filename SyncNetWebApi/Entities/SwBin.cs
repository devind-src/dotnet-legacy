using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_bins" — Card &gt; Base &gt; BIN. bin_nr is varchar(16) NOT NULL (PK,
    /// manual — legacy HTML form used maxlength=12, narrower than the real column, not
    /// replicated here). group_id is a real FK (ON DELETE NO ACTION) to sw_group.group_id.
    /// bin_desc is varchar(20) (legacy form maxlength=30, also narrower than real column).
    /// Real table also has status/created_by/created_dt/updated_by/updated_dt columns, never
    /// mapped or written by the legacy model/form — dead columns, not mapped here.</summary>
    [Table("sw_bins", Schema = "public")]
    public class SwBin
    {
        [Key]
        [MaxLength(16)]
        public string bin_nr { get; set; } = string.Empty;
        public int group_id { get; set; }
        [MaxLength(20)]
        public string? bin_desc { get; set; }
    }
}
