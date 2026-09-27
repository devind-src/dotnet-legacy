using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductBins
{
    public class UpdateProductBinRequest
    {
        [Required, MaxLength(16)]
        public string Bin { get; set; } = string.Empty;

        [MaxLength(6)]
        public string? Cbc { get; set; }

        [MaxLength(50)]
        public string? BankName { get; set; }
    }
}
