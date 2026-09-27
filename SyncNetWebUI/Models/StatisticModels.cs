namespace SyncNetWasm.Models
{
    // Statistics menu (Phase: demo/simulasi) — semua halaman di menu ini hanya menampilkan data
    // random client-side, tidak ada query DB nyata (belum ada modul transaksi/merchant real).
    // Ported dari shape SyncNetBlazorServer's Models/Statistic/*.cs.
    public class StatOpsByDate
    {
        public DateOnly DateTran { get; set; }
        public int MonthNumber { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public long SuccessTran { get; set; }
        public long FailedTran { get; set; }
        public long Total => SuccessTran + FailedTran;
    }

    public class StatFinByDate
    {
        public DateOnly DateTran { get; set; }
        public int MonthNumber { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public long TotalTran { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalFee { get; set; }
        public decimal TotalFeeSwitch { get; set; }
    }

    public class StatOpsRejected
    {
        public string RespCode { get; set; } = string.Empty;
        public string RespDesc { get; set; } = string.Empty;
        public long TotalRejected { get; set; }
    }

    public class StatOpsActivity
    {
        public string GroupName { get; set; } = string.Empty;
        public string TerminalId { get; set; } = string.Empty;
        public string LoketName { get; set; } = string.Empty;
        public Dictionary<string, long> ListRecord { get; set; } = [];
    }

    public class StatOpsByProduct
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public Dictionary<string, long> ListRecord { get; set; } = [];
        public long Total => ListRecord.Values.Sum();
    }
}
