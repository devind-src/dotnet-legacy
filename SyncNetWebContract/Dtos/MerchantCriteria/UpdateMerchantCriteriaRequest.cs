using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.MerchantCriteria
{
    /// <summary>Criteria is immutable after create — legacy renders it readonly on edit.</summary>
    public class UpdateMerchantCriteriaRequest
    {
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
