using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardGroups
{
    /// <summary>GroupName is immutable after create (readonly in the legacy edit form) — only
    /// InstId can be changed.</summary>
    public class UpdateCardGroupRequest
    {
        [MaxLength(6)]
        public string? InstId { get; set; }
    }
}
