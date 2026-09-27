using SWTCoreLab.DbRepository;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Message;
using System.Data;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    class VaResult
    {
        public int ret;
        public QueryModel query = new();
        public Response rsp = new();

        public VaResult()
        {
            ret = 0;
        }

        public VaResult(int rc)
        {
            ret = rc;
        }
    }

    class VaInfo
    {
        public bool bfound;
        public string acc_number;
        public long balance;
        public long min_balance;

        public VaInfo()
        {
            bfound = false;
            acc_number = "";
            balance = 0;
            min_balance = 0;
        }
    }

    class VirtualAccountService
    {
        public static async Task<VaInfo> getVaInfo(string acc_number)
        {
            VaInfo obj = new VaInfo();

            DataRow rec = await DbMgr.GetVaInfo(acc_number);
            if (rec != null)
            {
                obj.bfound = true;
                obj.acc_number = acc_number;
                obj.min_balance = NbConvert.ToLong(rec["min_balance"].ToString());
                obj.balance = NbConvert.ToLong(rec["balance"].ToString());
            }

            return obj;
        }

        public static async Task<VaResult> Debet(Message.Request req)
        {
            //check virtual account
            VaInfo va_info = await getVaInfo(req.virtual_account.acc_number);
            if (va_info.bfound == false)
                return new VaResult(-50); //does not have virtual account

            //amount debet va
            decimal amount = req.virtual_account.amount;
            if (amount == 0) amount = req.amount_tran;

            //set new balance
            decimal newbalance = va_info.balance - amount;

            //compare saldo
            if ((va_info.balance - va_info.min_balance) < amount)
                return new VaResult(-51);//saldo tidak cukup

            //set last balance
            req.virtual_account.balance = newbalance;

            //set query
            QueryModel query = new QueryModel();

            if (amount > 0)
            {
                query.sqltext = $@"UPDATE va_account SET balance=balance-@amount WHERE acc_nr=@acc_nr";
                query.param = new { amount = amount, acc_nr = va_info.acc_number };
            }

            //init response
            VaResult m = new VaResult();
            m.query = query;
            m.ret = 0;

            //ok
            return m;
        }

        public static async Task<VaResult> Credit(Message.Response rsp)
        {
            //check virtual account
            VaInfo va_info = await getVaInfo(rsp.virtual_account.acc_number);

            //does not have virtual account
            if (va_info.bfound == false) return new VaResult(-50);

            //amount credit
            decimal amount = rsp.virtual_account.amount;
            if (amount == 0) amount = rsp.amount_tran;

            //set new balance
            decimal newbalance = va_info.balance + amount;

            //set last balance
            rsp.virtual_account.balance = newbalance;

            //set query
            QueryModel query = new QueryModel();

            //update quota
            if (amount > 0)
            {
                query.sqltext = $@"UPDATE va_account SET balance=balance+@amount WHERE acc_nr=@acc_nr";
                query.param = new { amount = amount, acc_nr = va_info.acc_number };
            }

            //init response
            VaResult m = new VaResult();
            m.query = query;
            m.ret = 0;

            //ok
            return m;
        }

        public static async Task<VaResult> Reverse(Message.Response rsp)
        {
            //switchkey
            //string switch_key_org = rsp.original_data + rsp.merchant_id;
            string switch_key_org = DataHelper.GetSwitchKeyOrig(rsp.original_data, rsp.terminal_id);

            ////flag tran reversed
            //string tran_reversed = await DbMgr.getFlagTranReversed(switch_key_org);
            //if (string.IsNullOrEmpty(tran_reversed) == true) return new VaResult(-25);

            ////transaction has been reverse
            //if (tran_reversed == "1") return new VaResult(-94);

            if (await DbMgr.EligibleToReverse(switch_key_org) == false) return new VaResult(-25);

            //check virtual account
            VaInfo va_info = await getVaInfo(rsp.virtual_account.acc_number);

            //does not have virtual account
            if (va_info.bfound == false) return new VaResult(-50);

            //amount reverse
            decimal amount = rsp.virtual_account.amount;
            if (amount == 0) amount = rsp.amount_tran;

            //set new balance
            decimal newbalance = va_info.balance + amount;

            //set last balance
            rsp.virtual_account.balance = newbalance;

            //set query
            QueryModel query = new QueryModel();

            //update quota
            if (amount > 0)
            {
                query.sqltext = $@"UPDATE va_account SET balance=balance+@amount WHERE acc_nr=@acc_nr";
                query.param = new { amount = amount, acc_nr = va_info.acc_number };
            }

            //init response
            VaResult m = new VaResult();
            m.query = query;
            m.ret = 0;

            //ok
            return m;
        }

        public static async Task<VaResult> Topup(Message.Response rsp)
        {
            //check virtual account
            VaInfo va_info = await getVaInfo(rsp.virtual_account.acc_number);

            //does not have virtual account
            if (va_info.bfound == false) return new VaResult(-50);

            //amount topup
            decimal amount = rsp.virtual_account.amount;
            if (amount == 0) amount = rsp.amount_tran;

            //set new balance
            decimal newbalance = va_info.balance + amount;

            //set last balance
            rsp.virtual_account.balance = newbalance;

            //set query
            QueryModel query = new QueryModel();

            //validate amount
            if (amount > 0)
            {
                //update quota
                query.sqltext = $@"UPDATE va_account SET balance=balance+@amount WHERE acc_nr=@acc_nr";
                query.param = new { amount = amount, acc_nr = va_info.acc_number };
            }

            //init response
            VaResult m = new VaResult();
            m.query = query;
            m.rsp = rsp;
            m.ret = 0;

            //ok
            return m;
        }

        public static async Task<VaResult> Adjust(Message.Response rsp)
        {
            //check virtual account
            VaInfo va_info = await getVaInfo(rsp.virtual_account.acc_number);

            //does not have virtual account
            if (va_info.bfound == false) return new VaResult(-50);

            //amount adjustment
            decimal amount = rsp.virtual_account.amount;
            if (amount == 0) amount = rsp.amount_tran;

            //set new balance
            decimal newbalance = va_info.balance - amount;

            //set last balance
            rsp.virtual_account.balance = newbalance;

            //set query
            QueryModel query = new QueryModel();

            //validate amount
            if (amount > 0)
            {
                //update quota
                query.sqltext = $@"UPDATE va_account SET balance=balance-@amount WHERE acc_nr=@acc_nr";
                query.param = new { amount = amount, acc_nr = va_info.acc_number };
            }

            //init response
            VaResult m = new VaResult();
            m.query = query;
            m.rsp = rsp;
            m.ret = 0;

            //ok
            return m;
        }

        public static async Task<VaResult> getBalance(Message.Response rsp)
        {
            //check virtual account
            VaInfo va_info = await getVaInfo(rsp.virtual_account.acc_number);

            //does not have virtual account
            if (va_info.bfound == false) return new VaResult(-50);

            //set balance
            rsp.virtual_account.balance = va_info.balance;

            //init response
            VaResult m = new VaResult();
            m.rsp = rsp;
            m.ret = 0;

            //ok
            return m;
        }
    }
}
