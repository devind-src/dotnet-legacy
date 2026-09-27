namespace SyncNet.Constants
{
    class TranType
    {
        public const string MINISTATEMENT = "STM";
        public const string INQBALANCE = "BAL";
        public const string WITHDRAWAL = "WDL";
        public const string DEPOSIT = "DEP";

        public const string INQUIRY = "INQ";
        public const string PAYMENT = "PAY";
        public const string PURCHASE = "PUR";
        public const string TRANSFER = "TRF";

        public const string DEBET = "TDB";
        public const string CREDIT = "TCR";
        public const string ADJUSTMENT = "ADJ";

        public const string VOID = "VOD";
        public const string ADVICE = "ADV";
        public const string REFUND = "RFD";
        public const string REVERSAL = "REV";
        public const string AUTOREVERSAL = "ARV";

        //internal transaction
        public const string ADMIN = "ADM";
        public const string SETTLEMENT = "SET";
        public const string PINCHANGE = "PIN";
        public const string KEYCHANGE = "KEY";
        public const string CUTOVER = "CUT";
        public const string TERMLOGON = "LOG";

        public const string VTOPUP = "VCR";//credit
        public const string VADJUST = "VDB";//debet
        public const string VBALANCE = "VBA";//saldo
    }
}
