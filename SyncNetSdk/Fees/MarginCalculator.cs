using MessageFees = SyncNet.Message.Fees;

namespace SyncNet.Fees
{
    public class MarginCalculator
    {
        private readonly PriceRepository _priceRepository;

        public MarginCalculator(PriceRepository priceRepository)
        {
            _priceRepository = priceRepository;
        }

        public MessageFees Calculate(string merchantId, string productId, long denom, string supplierId)
        {
            var result = new MessageFees();

            if (_priceRepository.TryGetSupplierPrice(supplierId, productId, denom, out var price) == false)
                return result;

            int harga_beli = price.PurchasePrice;
            int harga_jual = price.SellingPrice;

            //check price to merchant
            if (_priceRepository.TryGetMerchantSellingPrice(merchantId, productId, denom, out int merchantHargaJual) == true)
            {
                //recalculate using price merchant
                harga_jual = merchantHargaJual;
            }

            int margin = harga_jual - harga_beli;

            result.total_fee = margin;
            result.switch_fee = margin;
            result.acquirer_fee = 0;
            result.biller_fee = 0;

            return result;
        }
    }
}
