using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.JobSchedules
{
    /// <summary>JobName is immutable after create — matches legacy UI, which renders it
    /// readonly on edit.</summary>
    public class UpdateJobScheduleRequest
    {
        [Required, RegularExpression("^[01]$")]
        public string FreqFlag { get; set; } = "0";

        [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Format harus HH:mm:ss")]
        public string FreqOnce { get; set; } = "00:00:00";

        public int? FreqNumber { get; set; }

        [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Format harus HH:mm:ss")]
        public string FreqStart { get; set; } = "00:00:00";

        [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Format harus HH:mm:ss")]
        public string FreqEnd { get; set; } = "23:59:59";

        [Required, RegularExpression("^(MW|BO)$")]
        public string RunAt { get; set; } = "MW";

        [MaxLength(100)]
        public string? AppPath { get; set; }

        public bool Active { get; set; } = true;
    }
}
