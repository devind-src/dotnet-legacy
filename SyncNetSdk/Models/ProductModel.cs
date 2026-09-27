namespace SyncNet.Models
{
    public class ProductModel
    {
        public class Product
        {
            public string ProductId { get; set; }
            public string ProductName { get; set; }
            public string ProductCategory { get; set; }
            public bool IsProductTopup { get; set; }
        }
        public class Mapping
        {
            public string ProductId { get; set; }
            public int Denom { get; set; }
            public string BillerCode { get; set; }
        }
    }
}
