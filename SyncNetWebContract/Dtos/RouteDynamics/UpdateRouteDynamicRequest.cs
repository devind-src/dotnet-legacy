using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteDynamics
{
    /// <summary>InstId is immutable after create — legacy renders it readonly on edit.</summary>
    public class UpdateRouteDynamicRequest
    {
        [Required, MaxLength(50)]
        public string Notes { get; set; } = string.Empty;
    }
}
