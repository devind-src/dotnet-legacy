using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductTransfers
{
    public class CreateProductTransferRequest
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(6)]
        public string BankCode { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string BankName { get; set; } = string.Empty;
    }
}
