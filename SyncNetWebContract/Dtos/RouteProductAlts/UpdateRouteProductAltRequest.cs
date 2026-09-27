using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteProductAlts
{
    /// <summary>InstId tidak bisa diubah setelah dibuat.</summary>
    public class UpdateRouteProductAltRequest
    {
        [Required]
        public int NodeId { get; set; }

        [Required, Range(2, 32000)]
        public int Priority { get; set; }

        [MaxLength(50)]
        public string? Notes { get; set; }

        public bool Active { get; set; } = true;
    }
}
