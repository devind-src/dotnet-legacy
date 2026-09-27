namespace SyncNetWasm.Models
{
    // Phase 3: filled with simulated data only (see Home.razor) — ported from the shape of
    // SyncNetBlazorServer's Index.razor (DashOneModel/DashTwoModel) so Phase 4's real
    // participant/merchant/terminal/transaction endpoints can populate these same models
    // without another UI rewrite.
    public class DashOneModel
    {
        public double SystemHealth { get; set; }
        public double SuccessRate { get; set; }
        public double Tps { get; set; }
        public int AvgRespTime { get; set; }
        public List<TopTenTransaction> ListTopTen { get; set; } = [];
        public List<ServiceStatus> ListServices { get; set; } = [];
    }

    public class TopTenTransaction
    {
        public string TerminalId { get; set; } = string.Empty;
        public string TranType { get; set; } = string.Empty;
        public long AmountTran { get; set; }
        public string RespCodeRsp { get; set; } = string.Empty;
        public DateTime TimeRequest { get; set; }
        public DateTime TimeResponse { get; set; }
        public double DurationMs => (TimeResponse - TimeRequest).TotalMilliseconds;
    }

    public class ServiceStatus
    {
        public string NodeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastEcho { get; set; }
    }

    public class DashTwoModel
    {
        public int TotalAgent { get; set; }
        public int TotalMerchant { get; set; }
        public int TotalSubMerchant { get; set; }

        public long TotalTransToday { get; set; }
        public long TotalTransYesterday { get; set; }

        public long TotalTransThisMonth { get; set; }
        public long TotalTransLastMonth { get; set; }

        public decimal TotalRevenueThisMonth { get; set; }
        public decimal TotalRevenueLastMonth { get; set; }

        public List<StatOpsByHour> DataChart24Hour { get; set; } = [];
        public List<StatOpsByType> DataChartByType { get; set; } = [];
        public List<StatOpsTopFive> DataTopFive { get; set; } = [];
    }

    public class StatOpsByHour
    {
        public int Hour { get; set; }
        public long SuccessTran { get; set; }
        public long FailedTran { get; set; }
        public long Total => SuccessTran + FailedTran;
    }

    public class StatOpsByType
    {
        public string TransactionType { get; set; } = string.Empty;
        public long Amount { get; set; }
    }

    public class StatOpsTopFive
    {
        public string Name { get; set; } = string.Empty;
        public long TotalTrans { get; set; }
    }
}
