using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteSources
{
    /// <summary>inst_id is varchar(30), notes is varchar(50) (both live-verified). NodeIdIn
    /// should be a node with category "1" (Merchant), NodeIdOut a node with category "0" or
    /// "2" (Both/Biller-Issuer) — matches legacy's filtered dropdowns, but (like legacy) not
    /// enforced server-side beyond "must exist in sw_nodes".</summary>
    public class CreateRouteSourceRequest
    {
        [Required]
        public int NodeIdIn { get; set; }

        [Required]
        public int NodeIdOut { get; set; }

        [MaxLength(30)]
        public string? InstId { get; set; }

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
