using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.PosnetBins
{
    /// <summary>Id is NOT supplied by the caller — computed server-side as max(id)+1, same
    /// non-serial-PK pattern as dashboard_role_menu.id (see SwPosnetBin entity note).</summary>
    public class CreatePosnetBinRequest
    {
        [Required, MaxLength(30)]
        public string ParticipantId { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Group is required.")]
        public int GroupId { get; set; }
    }
}
