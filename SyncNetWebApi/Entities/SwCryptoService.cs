using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_crypto_service" — HSM &gt; Service. hsm_desc is varchar(50), manual
    /// PK (string key, no identity concern). Real column "status" exists but is not mapped —
    /// product decision: no Active toggle for HSM Device/Service, unlike most other modules.
    /// Every other real column is used by the legacy form and is mapped here.</summary>
    [Table("sw_crypto_service", Schema = "public")]
    public class SwCryptoService
    {
        [Key]
        [MaxLength(50)]
        public string hsm_desc { get; set; } = string.Empty;

        public int? hsm_port { get; set; }
        public int? request_timeout { get; set; }
        public short? message_header { get; set; }

        [MaxLength(30)]
        public string? created_by { get; set; }
        public DateTime? created_dt { get; set; }
        [MaxLength(30)]
        public string? updated_by { get; set; }
        public DateTime? updated_dt { get; set; }
    }
}
