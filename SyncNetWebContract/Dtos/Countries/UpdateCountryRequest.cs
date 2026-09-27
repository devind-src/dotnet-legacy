using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Countries
{
    /// <summary>Name is immutable after create (it's the PK) — only the other fields change.
    /// code_alpha_2 is char(2), code_alpha_3 is char(3), code_numeric is integer NOT NULL —
    /// real column constraints.</summary>
    public class UpdateCountryRequest
    {
        [Required, MaxLength(2)]
        public string CodeAlpha2 { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string CodeAlpha3 { get; set; } = string.Empty;

        [Required]
        public int CodeNumeric { get; set; }

        public bool Active { get; set; } = true;
    }
}
