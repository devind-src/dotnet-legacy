using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "cms_issuer_contacts" — 1:1 child of cms_issuers (matched by `issuer`,
    /// no real FK constraint). PK `id`, GENERATED ALWAYS AS IDENTITY — never assign manually.
    /// Legacy's "Business Address" tab only ever reads/writes the *_1 triplet
    /// (address_1/city_1/postal_1) — address_2/city_2/postal_2 and address_3/city_3/postal_3
    /// exist in both the real table and the legacy C# model but are never touched by any form,
    /// so CreateAsync/UpdateAsync here only ever set the _1 triplet and never assign _2/_3
    /// (left null forever, same pattern as Node's untouched fields). Real table also has
    /// status/created_by/created_dt columns, never mapped by the legacy model — dead columns,
    /// not mapped here.</summary>
    [Table("cms_issuer_contacts", Schema = "public")]
    public class CmsIssuerContact
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [MaxLength(30)]
        public string issuer { get; set; } = string.Empty;
        [MaxLength(50)]
        public string? contact_name { get; set; }
        [MaxLength(30)]
        public string? phone { get; set; }
        [MaxLength(30)]
        public string? fax { get; set; }
        [MaxLength(30)]
        public string? mobile { get; set; }
        [MaxLength(50)]
        public string? email { get; set; }
        [MaxLength(50)]
        public string? address_1 { get; set; }
        [MaxLength(50)]
        public string? city_1 { get; set; }
        [MaxLength(5)]
        public string? postal_1 { get; set; }
        [MaxLength(50)]
        public string? address_2 { get; set; }
        [MaxLength(50)]
        public string? city_2 { get; set; }
        [MaxLength(5)]
        public string? postal_2 { get; set; }
        [MaxLength(50)]
        public string? address_3 { get; set; }
        [MaxLength(50)]
        public string? city_3 { get; set; }
        [MaxLength(5)]
        public string? postal_3 { get; set; }
        public DateTime? last_update { get; set; }
        [MaxLength(30)]
        public string? update_by { get; set; }
    }
}
