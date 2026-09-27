namespace SyncNetApi.Dtos.JobFees
{
    public record JobFeePreviewRowDto(
        int RowNumber, string ProductId, string? ProductName, string GroupName, string SubgroupName,
        string MerchantId, string SubmerchantId, string FeeType,
        decimal FeeTotal, decimal FeeAcq, decimal FeeIss, decimal FeeSwt,
        bool IsValid, string? Error);
}
