using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "va_group" — Virtual Account &gt; Group. group_name is the PK (varchar(50),
    /// manual, not auto-generated). Referenced by va_account.group_name via a real FK
    /// (ON DELETE NO ACTION, verified live) — delete must guard against accounts still
    /// pointing at the group (409, not cascade), same pattern as Card &gt; Base &gt; Group.</summary>
    [Table("va_group", Schema = "public")]
    public class VaGroup
    {
        [Key]
        [MaxLength(50)]
        public string group_name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? notes { get; set; }
    }
}
