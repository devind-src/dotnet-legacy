using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteMargins
{
    /// <summary>InstId is immutable after create — legacy renders it readonly on edit.</summary>
    public class UpdateRouteMarginRequest
    {
        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
