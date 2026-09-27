namespace SyncNetApi.Dtos.ProductPostpaidFees
{
    /// <summary>ProductName/MerchantName/SubmerchantName are resolved via join for display —
    /// mirrors legacy VwFees (inner join Product Master, left join Merchant/SubMerchant since
    /// those relations are optional). Acq = Fee Loket, Mer = Fee Mitra, Bil = Fee Biller,
    /// Swt = fee perusahaan. RoutingMode null di baris CA = Ikuti Default; EffectiveRoutingMode
    /// adalah mode yang benar-benar berlaku (baris CA atau baris default produk).</summary>
    public record ProductPostpaidFeeDto(
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
        decimal? PercentFeeSwt,
        int? FixedFeeMer = null,
        int? FixedFeeBil = null,
        decimal? PercentFeeMer = null,
        decimal? PercentFeeBil = null,
        string? RoutingMode = null,
        int? StaticNodeId = null,
        string? StaticNodeName = null,
        string EffectiveRoutingMode = "STATIC");
}
