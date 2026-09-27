using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.PosnetBins
{
    /// <summary>ParticipantId is immutable after create (matches legacy UI, disabled on
    /// edit) — only GroupId can change.</summary>
    public class UpdatePosnetBinRequest
    {
        [Range(1, int.MaxValue, ErrorMessage = "Group is required.")]
        public int GroupId { get; set; }
    }
}
