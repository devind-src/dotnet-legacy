namespace SyncNetApi.Dtos.Monitoring
{
    public record MonitoringFileDto(string FileName, DateTime LastModified, long SizeBytes);
}
