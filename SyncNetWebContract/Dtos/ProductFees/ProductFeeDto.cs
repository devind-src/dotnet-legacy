namespace SyncNetApi.Dtos.ProductFees
{
    /// <summary>ProductName/MerchantName/SubmerchantName are resolved via join for display —
    /// mirrors legacy VwFees (inner join Product Master, left join Merchant/SubMerchant since
    /// those relations are optional).</summary>
    public record ProductFeeDto(
        long Id,
        string? ProductId,
        string? ProductName,
        string? MerchantId,
        string? MerchantName,
        string? SubmerchantId,
        string? SubmerchantName,
        string? GroupName,
        string? SubgroupName,
        string FeeType,
        int? FixedFee,
        int? FixedFeeAcq,
        int? FixedFeeIss,
        int? FixedFeeSwt,
        decimal? PercentFee,
        decimal? PercentFeeAcq,
        decimal? PercentFeeIss,
        decimal? PercentFeeSwt);
}
