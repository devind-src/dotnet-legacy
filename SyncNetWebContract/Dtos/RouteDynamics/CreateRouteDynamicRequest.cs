using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteDynamics
{
    /// <summary>InstId (product code) is varchar(11), live-verified.</summary>
    public class CreateRouteDynamicRequest
    {
        [Required, MaxLength(11)]
        public string InstId { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
