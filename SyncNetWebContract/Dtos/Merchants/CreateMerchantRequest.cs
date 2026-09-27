using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Merchants
{
    public class CreateMerchantRequest
    {
        [Required, MaxLength(30)]
        public string MerchantId { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ParticipantId { get; set; } = string.Empty;

        [MaxLength(10)]
        public string? MccCode { get; set; }

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

        [MaxLength(100)]
        public string? Person { get; set; }

        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(100)]
        public string? Branch { get; set; }

        [MaxLength(100)]
        public string? DestBank { get; set; }

        [MaxLength(30)]
        public string? AccNumber { get; set; }

        [MaxLength(100)]
        public string? OwnerName { get; set; }

        public bool Active { get; set; } = true;

        [MaxLength(100)]
        public string? VaName { get; set; }

        [MaxLength(30)]
        public string? DateJoin { get; set; }

        [MaxLength(50)]
        public string? Brand { get; set; }

        [MaxLength(1)]
        public string? VirtualAccount { get; set; }

        [MaxLength(30)]
        public string? MidExt { get; set; }

        [MaxLength(30)]
        public string? Nmid { get; set; }

        // Key Management (manual entry — see SwMerchantKey entity note)
        [MaxLength(2)]
        public string? KeyLength { get; set; }

        [MaxLength(2)]
        public string? PinblockFormat { get; set; }

        [MaxLength(48)]
        public string? MasterKey { get; set; }

        [MaxLength(16)]
        public string? MasterKcv { get; set; }

        [MaxLength(48)]
        public string? KeyUnderLmk { get; set; }

        [MaxLength(48)]
        public string? KeyUnderZmk { get; set; }

        [MaxLength(16)]
        public string? KeyCheckValue { get; set; }
    }
}
