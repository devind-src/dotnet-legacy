using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.SupportTeams
{
    /// <summary>Name is immutable after create — matches legacy UI, which renders it readonly
    /// on edit (SupportTeamDetail.razor). Only Region/Active can change.</summary>
    public class UpdateSupportTeamRequest
    {
        [Required, MaxLength(30)]
        public string Region { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
