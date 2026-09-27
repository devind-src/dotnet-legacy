using System.Collections.Generic;

namespace SyncNetApi.Dtos.JobFees
{
    public record JobFeePreviewResultDto(int TotalRows, int ValidRows, int InvalidRows, List<JobFeePreviewRowDto> Rows);
}
