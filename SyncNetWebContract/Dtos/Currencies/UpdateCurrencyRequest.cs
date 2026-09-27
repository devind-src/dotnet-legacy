using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Currencies
{
    /// <summary>CurrencyCode is immutable after create (it's the PK). alpha_code is char(3)
    /// nullable, name is varchar(20), nr_decimals/rate are NOT NULL — real column constraints.</summary>
    public class UpdateCurrencyRequest
    {
        [MaxLength(3)]
        public string? AlphaCode { get; set; }

        [Required, MaxLength(20)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int NrDecimals { get; set; }

        [Required]
        public double Rate { get; set; }

        public bool Active { get; set; } = true;
    }
}
