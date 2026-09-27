using System;

namespace SyncNet.Models
{
    public class FailoverModel
    {
        public enum SupplierStatusEnum { ACTIVE, DOWN, SUSPECT }

        // alasan blokir (sw_routes_supplier_status.block_reason, sw_routes_failover_log.reason)
        public static class BlockReason
        {
            public const string LINK_DOWN = "LINK_DOWN";
            public const string TIMEOUT = "TIMEOUT";
            public const string RC_FAILED = "RC_FAILED";
            public const string PENDING = "PENDING";
            public const string LATENCY = "LATENCY";
        }

        public class SupplierStatus
        {
            public string SupplierId { get; set; }
            public SupplierStatusEnum Status { get; set; } = SupplierStatusEnum.ACTIVE;
            public string LastRcCode { get; set; }
            public int ConsecutiveSuspectCount { get; set; } //counter timeout
            public int ConsecutiveFailedCount { get; set; }
            public int ConsecutivePendingCount { get; set; }
            public int ConsecutiveLatencyCount { get; set; }
            public string BlockReason { get; set; }
            public int? LastLatencyMs { get; set; }
            public int RetryCount { get; set; }
            public DateTime? LastTranDt { get; set; }
            public DateTime? BlockedSince { get; set; }
            public DateTime? BlockedUntil { get; set; }
            public string UpdatedBy { get; set; }
            public DateTime? UpdatedDt { get; set; }
        }

        // RcSuspect/MaxConsecutiveSuspect/SuspectCooldownMinutes = kategori timeout.
        // RcFailed, RcPending, LatencyThresholdMs kosong = kategori nonaktif.
        public class Config
        {
            public string RoutingType { get; set; }
            public string InstId { get; set; }
            public string RcLinkDown { get; set; }
            public int? LinkDownCooldownMinutes { get; set; }
            public string RcSuspect { get; set; }
            public int MaxConsecutiveSuspect { get; set; }
            public int? SuspectCooldownMinutes { get; set; }
            public string RcFailed { get; set; }
            public int MaxConsecutiveFailed { get; set; } = 3;
            public int? FailedCooldownMinutes { get; set; }
            public string RcPending { get; set; }
            public int MaxConsecutivePending { get; set; } = 5;
            public int? PendingCooldownMinutes { get; set; }
            public int? LatencyThresholdMs { get; set; }
            public int MaxConsecutiveLatency { get; set; } = 3;
            public int? LatencyCooldownMinutes { get; set; }
            public bool IsActive { get; set; }

            public bool IsLinkDownCode(string rcCode) => ContainsCode(RcLinkDown, rcCode);
            public bool IsSuspectCode(string rcCode) => ContainsCode(RcSuspect, rcCode);
            public bool IsFailedCode(string rcCode) => ContainsCode(RcFailed, rcCode);
            public bool IsPendingCode(string rcCode) => ContainsCode(RcPending, rcCode);

            public bool IsLatencyActive => LatencyThresholdMs.HasValue == true && LatencyThresholdMs.Value > 0;

            public int? CooldownFor(string reason) => reason switch
            {
                BlockReason.LINK_DOWN => LinkDownCooldownMinutes,
                BlockReason.TIMEOUT => SuspectCooldownMinutes,
                BlockReason.RC_FAILED => FailedCooldownMinutes,
                BlockReason.PENDING => PendingCooldownMinutes,
                BlockReason.LATENCY => LatencyCooldownMinutes,
                _ => null
            };

            private static bool ContainsCode(string csv, string rcCode)
            {
                if (string.IsNullOrEmpty(csv) || string.IsNullOrEmpty(rcCode)) return false;

                foreach (var code in csv.Split(','))
                {
                    if (code.Trim() == rcCode.Trim()) return true;
                }

                return false;
            }
        }
    }
}
