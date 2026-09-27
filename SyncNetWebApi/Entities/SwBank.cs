using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_bank" — Configuration &gt; Base &gt; Bank. id was manually computed as
    /// max(id)+1 (legacy quirk); converted to a real Postgres IDENTITY column — DB now assigns
    /// it on insert. Still backs the read-only "Bank Name" dropdown on Merchant/Terminal forms
    /// via LookupsController.</summary>
    [Table("sw_bank", Schema = "public")]
    public class SwBank
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        public string? bank { get; set; }
        public string? cbc { get; set; }
        public string? status { get; set; }
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
