using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteProducts
{
    /// <summary>InstId is immutable after create — legacy renders it readonly on edit.</summary>
    public class UpdateRouteProductRequest
    {
        [Required]
        public int NodeId { get; set; }

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
