using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteFailoverConfigs
{
    public class CreateRouteFailoverConfigRequest : UpdateRouteFailoverConfigRequest
    {
        [Required, MaxLength(20)]
        public string RoutingType { get; set; } = "MARGIN";

        [MaxLength(11)]
        public string? InstId { get; set; }
    }
}
