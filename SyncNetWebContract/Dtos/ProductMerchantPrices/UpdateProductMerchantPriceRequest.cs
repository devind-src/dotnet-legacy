namespace SyncNetApi.Dtos.ProductMerchantPrices
{
    /// <summary>MerchantId/BillerCode are immutable after create — legacy renders both
    /// readonly on edit.</summary>
    public class UpdateProductMerchantPriceRequest
    {
        public int? Denom { get; set; }

        public int? HargaJual { get; set; }
    }
}
