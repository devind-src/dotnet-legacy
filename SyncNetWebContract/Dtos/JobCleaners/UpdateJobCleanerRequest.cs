using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.JobCleaners
{
    /// <summary>Entity is immutable after create — matches legacy UI, which renders it readonly
    /// on edit.</summary>
    public class UpdateJobCleanerRequest
    {
        [Required, Range(3, int.MaxValue, ErrorMessage = "Minimum period 3 hari")]
        public int Period { get; set; } = 3;

        [Required, MaxLength(50)]
        public string Description { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
