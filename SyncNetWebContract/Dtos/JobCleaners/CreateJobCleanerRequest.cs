using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.JobCleaners
{
    /// <summary>entity is varchar(30), description is varchar(50) — real column widths.
    /// Minimum period 3 days matches legacy validation (JobCleanerDetail.IsValid).</summary>
    public class CreateJobCleanerRequest
    {
        [Required, MaxLength(30)]
        public string Entity { get; set; } = string.Empty;

        [Required, Range(3, int.MaxValue, ErrorMessage = "Minimum period 3 hari")]
        public int Period { get; set; } = 3;

        [Required, MaxLength(50)]
        public string Description { get; set; } = string.Empty;
    }
}
