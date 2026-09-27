namespace SyncNetApi.Dtos.ProductMerchantPricing
{
    /// <summary>MerchantId/BillerCode are immutable after create — legacy renders both
    /// readonly on edit.</summary>
    public class UpdateProductMerchantPricingRequest
    {
        public int? Denom { get; set; }

        public int? HargaJual { get; set; }
    }
}
