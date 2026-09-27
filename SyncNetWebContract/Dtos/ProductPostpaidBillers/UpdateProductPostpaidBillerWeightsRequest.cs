using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPostpaidBillers
{
    /// <summary>Ubah bobot Load Balance semua biller satu produk sekaligus. Total bobot biller
    /// aktif wajib tepat 100.</summary>
    public class UpdateProductPostpaidBillerWeightsRequest
    {
        public List<ProductPostpaidBillerWeight> Weights { get; set; } = new();
    }

    public class ProductPostpaidBillerWeight
    {
        public int NodeId { get; set; }
        [Range(0, 100)]
        public int LbWeight { get; set; }
    }
}
