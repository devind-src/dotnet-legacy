using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.MerchantCriteria
{
    /// <summary>If SubgroupName is non-empty, GroupName is forced empty server-side before
    /// saving (legacy: "jika sub group diisi, maka group di set kosong") — mutually exclusive.
    /// The combination of Criteria+GroupName+SubgroupName must be unique. If
    /// FeeToMerchant+FeeToSubmerchant+FeeToSwitch is non-zero, it must total exactly 100.</summary>
    public class CreateMerchantCriteriaRequest
    {
        [Required, MaxLength(30)]
        public string Criteria { get; set; } = string.Empty;

        public decimal Mdr { get; set; }
        public decimal FeeFromIssuer { get; set; }
        public decimal FeeFromPartner { get; set; }
        public decimal FeeToMerchant { get; set; }
        public decimal FeeToSubmerchant { get; set; }
        public decimal FeeToSwitch { get; set; }

        [MaxLength(50)]
        public string Notes { get; set; } = string.Empty;

        [MaxLength(50)]
        public string GroupName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string SubgroupName { get; set; } = string.Empty;
    }
}
