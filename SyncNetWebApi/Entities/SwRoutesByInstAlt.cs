using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SyncNetApi.Entities
{
    /// <summary>Table "sw_routes_by_inst_alt" — panel detail Alternate Biller di menu Routing
    /// &gt; Product (`/route-product`, master-detail seperti Nodes &gt; Connections). Biller
    /// cadangan per produk bill payment, model primary-backup (bukan load balancer). Primary
    /// tetap di `sw_routes_by_inst`. `priority` minimal 2 (rank 1 = primary), unik per produk
    /// (constraint DEFERRABLE, jadi dua baris bisa bertukar dalam satu transaksi).</summary>
    [Table("sw_routes_by_inst_alt", Schema = "public")]
    public class SwRoutesByInstAlt
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }
        [MaxLength(11)]
        public string inst_id { get; set; } = string.Empty;
        public int node_id { get; set; }
        public short priority { get; set; }
        [MaxLength(50)]
        public string? notes { get; set; }
        /// <summary>Sharing fee (Rp) dari biller alternate untuk produk ini.</summary>
        public int? fee_sharing { get; set; }
        /// <summary>Bobot Load Balance (0-100).</summary>
        public short? lb_weight { get; set; }
        public string status { get; set; } = "1";
        [MaxLength(30)]
        public string? created_by { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? created_dt { get; set; }
        [MaxLength(30)]
        public string? updated_by { get; set; }
        [Column(TypeName = "timestamp without time zone")]
        public DateTime? updated_dt { get; set; }
    }
}
