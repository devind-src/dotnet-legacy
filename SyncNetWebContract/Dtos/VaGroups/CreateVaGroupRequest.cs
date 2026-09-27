using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.VaGroups
{
    public class CreateVaGroupRequest
    {
        [Required, MaxLength(50)]
        public string GroupName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Notes { get; set; }
    }
}
