using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.SupportTeams
{
    /// <summary>TeamId is NOT supplied by the caller — computed server-side as max(id)+1
    /// (sw_support_teams.team_id is not identity). name/region are varchar(30) — real column
    /// widths, both required to match legacy validation (SupportTeamDetail.IsValid).</summary>
    public class CreateSupportTeamRequest
    {
        [Required, MaxLength(30)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string Region { get; set; } = string.Empty;
    }
}
