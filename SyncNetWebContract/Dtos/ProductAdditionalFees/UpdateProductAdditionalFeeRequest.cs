using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductAdditionalFees
{
    /// <summary>GroupName is immutable after create — legacy renders it readonly on edit.</summary>
    public class UpdateProductAdditionalFeeRequest
    {
        [Required, Range(1, 100_000)]
        public int Fee { get; set; }
    }
}
