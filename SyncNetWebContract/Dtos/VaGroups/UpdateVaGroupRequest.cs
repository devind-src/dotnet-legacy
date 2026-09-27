using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.VaGroups
{
    /// <summary>GroupName is not included — immutable after create (readonly in the legacy
    /// Edit form).</summary>
    public class UpdateVaGroupRequest
    {
        [MaxLength(100)]
        public string? Notes { get; set; }
    }
}
