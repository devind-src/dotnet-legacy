using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.PublicHolidays
{
    /// <summary>holiday_date is varchar(10), format "yyyy-MM-dd" (matches legacy Save()).
    /// holiday_name is varchar(20).</summary>
    public class CreatePublicHolidayRequest
    {
        [Required, RegularExpression(@"^\d{4}-\d{2}-\d{2}$", ErrorMessage = "Format harus yyyy-MM-dd")]
        public string HolidayDate { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string HolidayName { get; set; } = string.Empty;
    }
}
