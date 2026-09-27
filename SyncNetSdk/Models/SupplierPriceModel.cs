namespace SyncNet.Models
{
    public class SupplierPriceModel
    {
        public string SupplierId { get; set; }
        public int PurchasePrice { get; set; }
        public int SellingPrice { get; set; }
        public int Margin { get; set; }
        public int? Priority { get; set; }
        public int LbWeight { get; set; }
    }
}
