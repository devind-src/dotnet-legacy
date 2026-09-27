namespace SyncNetApi.Dtos.MerchantCriteria
{
    public record MerchantCriteriaDto(long Id, string Criteria, decimal Mdr, decimal FeeFromIssuer, decimal FeeFromPartner, decimal FeeToMerchant, decimal FeeToSubmerchant, decimal FeeToSwitch, string Notes, string GroupName, string SubgroupName);
}
