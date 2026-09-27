namespace SyncNetApi.Dtos.JobFees
{
    /// <summary>FeeType is the human-readable label ("Fixed"/"Percent"), translated from the
    /// stored "0"/"1" code.</summary>
    public record JobFeeDetailDto(
        long Id,
        string? ProductId,
        string? GroupName,
        string? SubgroupName,
        string? MerchantId,
        string? SubmerchantId,
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
