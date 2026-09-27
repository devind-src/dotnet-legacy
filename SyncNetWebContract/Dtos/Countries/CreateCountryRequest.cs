using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Countries
{
    /// <summary>name is varchar(50), code_alpha_2 is char(2), code_alpha_3 is char(3),
    /// code_numeric is integer NOT NULL — real column constraints.</summary>
    public class CreateCountryRequest
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(2)]
        public string CodeAlpha2 { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string CodeAlpha3 { get; set; } = string.Empty;

        [Required]
        public int CodeNumeric { get; set; }
    }
}
