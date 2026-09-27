namespace SWTCoreLab.Cams
{
    class CamsTranType
    {
        public enum ListTranType
        {
            None = 0,
            Inquiry = 1,
            Purchase = 2,
            Cash = 3,
            Payment = 4,
            Transfer = 5
        }

        public ListTranType getTranType(string tran_type)
        {
            ListTranType t = new ListTranType();

            switch (tran_type)
            {
                case Common.TranType.INQBALANCE:
                    t = ListTranType.Inquiry;
                    break;
                case Common.TranType.WITHDRAWAL:
                    t = ListTranType.Cash;
                    break;
                case Common.TranType.PURCHASE:
                    t = ListTranType.Purchase;
                    break;
                case Common.TranType.TRANSFER:
                    t = ListTranType.Transfer;
                    break;
                case Common.TranType.PAYMENT:
                    t = ListTranType.Payment;
                    break;
            }

            return t;
        }

        public bool IsTranTypeSupported(string tran_type)
        {
            bool ret = false;

            switch (tran_type)
            {
                //tran type supported for cams
                case Common.TranType.INQBALANCE:
                case Common.TranType.WITHDRAWAL:
                case Common.TranType.TRANSFER:
                case Common.TranType.PAYMENT:
                case Common.TranType.PURCHASE:
                    ret = true;
                    break;
            }

            return ret;
        }
    }
}
