namespace SyncNetApi.Dtos.Monitoring
{
    /// <summary>A paginated slice of a log/trace file's lines — never the whole file (see
    /// MonitoringOptions.PageSize). <paramref name="HasMore"/> tells the caller whether another
    /// page exists at <paramref name="Offset"/> + line count.</summary>
    public record MonitoringFileContentDto(string FileName, IReadOnlyList<string> Lines, int Offset, int PageSize, bool HasMore, long SizeBytes);
}
