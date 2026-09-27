using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.Terminals
{
    public class UpdateTerminalRequest
    {
        // General
        [Required, MaxLength(15)]
        public string MerchantId { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? SubMerchantId { get; set; }

        [MaxLength(30)]
        public string? StoreId { get; set; }

        [MaxLength(50)]
        public string? LoketName { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        [MaxLength(30)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public bool Active { get; set; } = true;

        // Group
        [MaxLength(50)]
        public string? GroupName { get; set; }

        [MaxLength(50)]
        public string? SubgroupName { get; set; }

        [MaxLength(50)]
        public string? Criteria { get; set; }

        [MaxLength(30)]
        public string? Nmid { get; set; }

        [MaxLength(30)]
        public string? Mpan { get; set; }

        [MaxLength(30)]
        public string? ChatId { get; set; }

        // Device
        [MaxLength(30)]
        public string? Brand { get; set; }

        [MaxLength(30)]
        public string? Type { get; set; }

        [Required, MaxLength(50)]
        public string SerialNumber { get; set; } = string.Empty;

        public bool EnableInit { get; set; }

        [MaxLength(16)]
        public string? InitId { get; set; }

        [MaxLength(10)]
        public string? InstDate { get; set; }

        // Security
        public bool Security { get; set; }

        [MaxLength(15)]
        public string? IpAddress { get; set; }

        [MaxLength(15)]
        public string? MacAddress { get; set; }

        // Bank
        [MaxLength(100)]
        public string? BankName { get; set; }

        [MaxLength(50)]
        public string? BankBranch { get; set; }

        [MaxLength(6)]
        public string? BankCode { get; set; }

        [MaxLength(30)]
        public string? BankAccNumber { get; set; }

        [MaxLength(30)]
        public string? BankAccName { get; set; }

        [MaxLength(30)]
        public string? VirtualAccount { get; set; }

        [MaxLength(100)]
        public string? VaName { get; set; }

        // Key Management (manual entry — see TerminalDto note)
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
