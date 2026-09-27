using System;
using System.Collections.Generic;
using System.Text;

namespace API.Agent.Common
{
    class TranType
    {
        public const string INQBALANCE = "bal";
        public const string WITHDRAWAL = "wdl";

        public const string INQUIRY = "inq";
        public const string PAYMENT = "pay";
        public const string PURCHASE = "pur";
        public const string TRANSFER = "trf";

        public const string ADVICE = "adv";
        public const string REFUND = "rfd";
        public const string REVERSAL = "rev";

        //internal transaction
        public const string ADMIN = "adm";
        public const string PINCHANGE = "pin";
        public const string KEYCHANGE = "key";

        public const string VTOPUP = "vcr";//credit
        public const string VADJUST = "vdb";//debet
        public const string VBALANCE = "vba";//saldo
    }
}
