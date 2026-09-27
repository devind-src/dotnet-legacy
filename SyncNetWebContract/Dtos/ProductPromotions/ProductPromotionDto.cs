using System;

namespace SyncNetApi.Dtos.ProductPromotions
{
    /// <summary>ProductName is resolved via join for display — mirrors legacy VwFeesPromo
    /// (inner join Product Master).</summary>
    public record ProductPromotionDto(
        long Id,
        string? ProductId,
        string? ProductName,
        string? PromoCode,
        string? PromoDesc,
        DateTime DateStart,
        DateTime DateEnd,
        string FeeType,
        int? FixedFee,
        int? FixedFeeAcq,
        int? FixedFeeIss,
        int? FixedFeeSwt,
        decimal? PercentFee,
        decimal? PercentFeeAcq,
        decimal? PercentFeeIss,
        decimal? PercentFeeSwt,
        bool IsActive);
}
