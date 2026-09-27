using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.BusinessDates
{
    /// <summary>BusinessCalendar is immutable after create — matches legacy UI, which renders
    /// it readonly on edit. current_bsn_date/previous_bsn_date are never touched here (see
    /// entity note) — updating this record must not disturb whatever the external batch/EOD
    /// process has written into those two columns.</summary>
    public class UpdateBusinessDateRequest
    {
        [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Format harus HH:mm:ss")]
        public string TimeCutover { get; set; } = "00:00:00";

        public bool EnableClosing { get; set; }

        [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Format harus HH:mm:ss")]
        public string TimeStart { get; set; } = "00:00:00";

        [Required, RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$", ErrorMessage = "Format harus HH:mm:ss")]
        public string TimeEnd { get; set; } = "00:00:00";

        public bool Sun { get; set; } = true;
        public bool Mon { get; set; } = true;
        public bool Tue { get; set; } = true;
        public bool Wed { get; set; } = true;
        public bool Thu { get; set; } = true;
        public bool Fri { get; set; } = true;
        public bool Sat { get; set; } = true;

        public bool Active { get; set; } = true;
    }
}
