using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Stores
{
    public class CreateStoreRequest
    {
        [Required, MaxLength(30)]
        public string StoreId { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string MerchantId { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? Zipcode { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(30)]
        public string? Fax { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        public bool Active { get; set; } = true;

        [MaxLength(30)]
        public string? DateJoin { get; set; }
    }
}
