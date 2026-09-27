using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardGroups
{
    /// <summary>GroupId is NOT supplied by the caller — computed server-side as
    /// max(group_id)+1 (sw_group.group_id is not identity).</summary>
    public class CreateCardGroupRequest
    {
        [Required, MaxLength(20)]
        public string GroupName { get; set; } = string.Empty;

        [MaxLength(6)]
        public string? InstId { get; set; }
    }
}
