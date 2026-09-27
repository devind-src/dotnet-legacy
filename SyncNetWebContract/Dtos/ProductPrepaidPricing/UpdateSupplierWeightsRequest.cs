using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.ProductPrepaidPricing
{
    /// <summary>Ubah LB weight semua supplier satu produk + denom sekaligus (mode Load Balance).
    /// Total bobot supplier aktif pada denom itu wajib tepat 100.</summary>
    public class UpdateSupplierWeightsRequest
    {
        public int? Denom { get; set; }
        public List<SupplierWeight> Weights { get; set; } = new();
    }

    public class SupplierWeight
    {
        /// <summary>id baris sw_margin_supplier.</summary>
        public int Id { get; set; }
        [Range(0, 100)]
        public int LbWeight { get; set; }
    }
}
