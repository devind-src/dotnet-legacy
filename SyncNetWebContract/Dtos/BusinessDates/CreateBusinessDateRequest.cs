using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.BusinessDates
{
    /// <summary>business_calendar is varchar(20). time_cutover/time_start/time_end are
    /// varchar(8), format HH:mm:ss (matches legacy IsTimeValid validation). current_bsn_date/
    /// previous_bsn_date are NOT settable here — managed by an external batch/EOD process (see
    /// entity note), left null on create.</summary>
    public class CreateBusinessDateRequest
    {
        [Required, MaxLength(20)]
        public string BusinessCalendar { get; set; } = string.Empty;

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
    }
}
