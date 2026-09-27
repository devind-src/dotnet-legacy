using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.CardIssuers
{
    /// <summary>Issuer (PK) is immutable after create — not part of this request.</summary>
    public class UpdateCardIssuerRequest
    {
        [Required, MaxLength(6)]
        public string InstId { get; set; } = string.Empty;

        [Required, MaxLength(3)]
        public string Currency { get; set; } = string.Empty;

        public bool AuthService { get; set; }
        public bool VelocityPerTran { get; set; }
        public bool VelocityDaily { get; set; }
        public bool VelocityWeekly { get; set; }
        public bool VelocityMonthly { get; set; }

        public int? MaxPinTries { get; set; }

        [MaxLength(50)]
        public string? ContactName { get; set; }
        [MaxLength(30)]
        public string? Phone { get; set; }
        [MaxLength(30)]
        public string? Fax { get; set; }
        [MaxLength(30)]
        public string? Mobile { get; set; }
        [MaxLength(50)]
        public string? Email { get; set; }
        [MaxLength(50)]
        public string? BusinessAddress { get; set; }
        [MaxLength(50)]
        public string? City { get; set; }
        [MaxLength(5)]
        public string? PostalCode { get; set; }

        [MaxLength(1)]
        public string? KeyLength { get; set; }
        [MaxLength(2)]
        public string? PinblockFormat { get; set; }
        [MaxLength(49)]
        public string? MasterKey { get; set; }
        [MaxLength(16)]
        public string? MasterKcv { get; set; }
        [MaxLength(49)]
        public string? KeyUnderLmk { get; set; }
        [MaxLength(49)]
        public string? KeyUnderZmk { get; set; }
        [MaxLength(16)]
        public string? KeyCheckValue { get; set; }
    }
}
