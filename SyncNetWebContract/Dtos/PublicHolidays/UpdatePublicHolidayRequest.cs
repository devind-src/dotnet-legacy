using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.PublicHolidays
{
    /// <summary>HolidayDate is immutable after create (it's the PK) — matches legacy UI, which
    /// renders it readonly on edit.</summary>
    public class UpdatePublicHolidayRequest
    {
        [Required, MaxLength(20)]
        public string HolidayName { get; set; } = string.Empty;

        public bool Active { get; set; } = true;
    }
}
