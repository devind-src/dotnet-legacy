using System;

namespace SyncNetApi.Dtos.ProductPromos
{
    /// <summary>ProductName is resolved via join for display — mirrors legacy VwFeesPromo
    /// (inner join Product Master).</summary>
    public record ProductPromoDto(
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
