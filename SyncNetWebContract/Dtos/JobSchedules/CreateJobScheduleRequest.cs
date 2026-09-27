using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.JobSchedules
{
    /// <summary>JobId is NOT supplied by the caller — computed server-side as max(id)+1
    /// (sw_jobs.job_id is not identity). freq_once/freq_start/freq_end are varchar(8), format
    /// HH:mm:ss (matches legacy IsTimeValid validation). FreqFlag: "0" Occurs Once / "1" Occurs
    /// Every. RunAt: "MW" Middleware / "BO" Back Office.</summary>
    public class CreateJobScheduleRequest
    {
        [Required, MaxLength(50)]
        public string JobName { get; set; } = string.Empty;

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
    }
}
