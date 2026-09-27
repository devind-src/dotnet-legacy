namespace SyncNetApi.Dtos.ProductFeeTierings
{
    /// <summary>ProductName is resolved via join for display — mirrors legacy VwFeesTiering.</summary>
    public record ProductFeeTieringDto(
        long Id,
        string? ProductId,
        string? ProductName,
        int TierLevel,
        long? MinTran,
        long? MaxTran,
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
