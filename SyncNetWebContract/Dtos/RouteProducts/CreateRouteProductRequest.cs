using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteProducts
{
    /// <summary>InstId (product code) is varchar(11), live-verified.</summary>
    public class CreateRouteProductRequest
    {
        [Required, MaxLength(11)]
        public string InstId { get; set; } = string.Empty;

        [Required]
        public int NodeId { get; set; }

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
