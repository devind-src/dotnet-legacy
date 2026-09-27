namespace SyncNetApi.Common
{
    /// <summary>Mirrors legacy Constants/TranTypeVA.cs — va_trans_request.tran_type values.</summary>
    public static class VaTranType
    {
        public const string Topup = "80";
        public const string AdjCredit = "90";
        public const string AdjDebet = "91";

        public static string GetName(string? id) => id switch
        {
            Topup => "Topup",
            AdjCredit => "Adj Credit",
            AdjDebet => "Adj Debet",
            _ => string.Empty
        };
    }

    /// <summary>Mirrors legacy Constants/TranStatusVA.cs — va_trans_request.status values.</summary>
    public static class VaTranStatus
    {
        public const string Request = "0";
        public const string Approved = "1";
        public const string Rejected = "2";

        public static string GetName(string? id) => id switch
        {
            Request => "Request",
            Approved => "Approved",
            Rejected => "Rejected",
            _ => string.Empty
        };
    }

    /// <summary>Mirrors the subset of legacy Constants/TranType.cs used by VA Approval when
    /// writing to sw_trans_pg.tran_type ("VCR"=credit/topup, "VDB"=debet/adjustment).</summary>
    public static class SwTranType
    {
        public const string VaTopupCredit = "VCR";
        public const string VaAdjustDebet = "VDB";
    }
}
