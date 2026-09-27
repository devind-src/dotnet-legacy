namespace SyncNetApi.Dtos.Routing
{
    /// <summary>Hasil tombol "Terapkan Perubahan": RESYNC dikirim ke setiap aplikasi yang memakai
    /// routing (konfigurasi Routing:ResyncApps).</summary>
    public record RoutingApplyResultDto(IReadOnlyList<RoutingApplyItemDto> Results)
    {
        public bool AllSucceeded => Results.Count > 0 && Results.All(r => r.Success);
    }

    public record RoutingApplyItemDto(string AppName, bool Success, string Message);
}
