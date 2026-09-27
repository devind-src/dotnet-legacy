using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteSources
{
    /// <summary>NodeIdIn/NodeIdOut are immutable after create — legacy renders both selects
    /// disabled on edit.</summary>
    public class UpdateRouteSourceRequest
    {
        [MaxLength(30)]
        public string? InstId { get; set; }

        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
