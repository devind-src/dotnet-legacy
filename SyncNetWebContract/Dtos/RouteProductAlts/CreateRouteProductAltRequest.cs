using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteProductAlts
{
    public class CreateRouteProductAltRequest
    {
        [Required, MaxLength(11)]
        public string InstId { get; set; } = string.Empty;

        [Required]
        public int NodeId { get; set; }

        /// <summary>Minimal 2. Kosong = urutan berikutnya.</summary>
        [Range(2, 32000)]
        public int? Priority { get; set; }

        [MaxLength(50)]
        public string? Notes { get; set; }

        public bool Active { get; set; } = true;
    }
}
