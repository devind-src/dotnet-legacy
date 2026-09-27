using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Currencies
{
    /// <summary>currency_code is char(3), alpha_code is char(3) nullable, name is varchar(20),
    /// nr_decimals/rate are NOT NULL — real column constraints.</summary>
    public class CreateCurrencyRequest
    {
        [Required, MaxLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        [MaxLength(3)]
        public string? AlphaCode { get; set; }

        [Required, MaxLength(20)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int NrDecimals { get; set; }

        [Required]
        public double Rate { get; set; }
    }
}
