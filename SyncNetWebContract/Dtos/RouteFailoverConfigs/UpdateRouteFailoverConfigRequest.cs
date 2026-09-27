using System.ComponentModel.DataAnnotations;

namespace SyncNetApi.Dtos.RouteFailoverConfigs
{
    /// <summary>Ambang health check per kategori. Daftar rc dipisah koma. RC Gagal, Pending, dan
    /// Latency boleh kosong (= kategori nonaktif). Cooldown kosong = hanya reset manual.</summary>
    public class UpdateRouteFailoverConfigRequest
    {
        // Link down: langsung DOWN
        [Required, MaxLength(50)]
        public string RcLinkDown { get; set; } = "91,1091";

        [Range(1, 100000)]
        public int? LinkDownCooldownMinutes { get; set; }

        // Timeout (kolom lama rc_suspect / max_consecutive_suspect / suspect_cooldown_minutes)
        [Required, MaxLength(50)]
        public string RcSuspect { get; set; } = "68,1068";

        [Range(1, 100)]
        public int MaxConsecutiveSuspect { get; set; } = 3;

        [Range(1, 100000)]
        public int? SuspectCooldownMinutes { get; set; }

        // RC Gagal
        [MaxLength(50)]
        public string? RcFailed { get; set; }

        [Range(1, 100)]
        public int MaxConsecutiveFailed { get; set; } = 3;

        [Range(1, 100000)]
        public int? FailedCooldownMinutes { get; set; }

        // Pending
        [MaxLength(50)]
        public string? RcPending { get; set; }

        [Range(1, 100)]
        public int MaxConsecutivePending { get; set; } = 5;

        [Range(1, 100000)]
        public int? PendingCooldownMinutes { get; set; }

        // Latency: ambang diinput dalam detik, disimpan dalam ms
        [Range(1, 300)]
        public decimal? LatencyThresholdSeconds { get; set; }

        [Range(1, 100)]
        public int MaxConsecutiveLatency { get; set; } = 3;

        [Range(1, 100000)]
        public int? LatencyCooldownMinutes { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
