using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteBins
{
    /// <summary>GroupId is the Card Group (`sw_group.group_id`) chosen from a dropdown — it
    /// becomes the PK of the created row, so one Card Group can only have one BIN route.</summary>
    public class CreateRouteBinRequest
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        public int NodeId { get; set; }
    }
}
