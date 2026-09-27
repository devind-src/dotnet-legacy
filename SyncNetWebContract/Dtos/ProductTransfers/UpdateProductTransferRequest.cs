using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductTransfers
{
    /// <summary>Name is immutable after create — legacy renders the select disabled on
    /// edit.</summary>
    public class UpdateProductTransferRequest
    {
        [Required, MaxLength(6)]
        public string BankCode { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string BankName { get; set; } = string.Empty;
    }
}
