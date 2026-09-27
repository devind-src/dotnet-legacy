using SWTCoreLab.Common;
using SWTCoreLab.DbEngine;
using SWTCoreLab.Library;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SWTCoreLab.Cams
{
    class CardLimitObj
    {
        public string issuer;
        public string product;
        public string pan_prefix;
        public string channel;

        //per tran
        public long amt_purchase_per_tran;
        public long amt_cash_per_tran;
        public long amt_payment_per_tran;
        public long amt_transfer_per_tran;

        //daily
        public int nr_inquiry_daily;
        public int nr_purchase_daily;
        public int nr_cash_daily;
        public int nr_payment_daily;
        public int nr_transfer_daily;
        public long amt_purchase_daily;
        public long amt_cash_daily;
        public long amt_payment_daily;
        public long amt_transfer_daily;

        //weekly
        public int nr_inquiry_weekly;
        public int nr_purchase_weekly;
        public int nr_cash_weekly;
        public int nr_payment_weekly;
        public int nr_transfer_weekly;
        public long amt_purchase_weekly;
        public long amt_cash_weekly;
        public long amt_payment_weekly;
        public long amt_transfer_weekly;

        //monthly
        public int nr_inquiry_monthly;
        public int nr_purchase_monthly;
        public int nr_cash_monthly;
        public int nr_payment_monthly;
        public int nr_transfer_monthly;
        public long amt_purchase_monthly;
        public long amt_cash_monthly;
        public long amt_payment_monthly;
        public long amt_transfer_monthly;

        public CardLimitObj()
        {
            issuer = string.Empty;
            product = string.Empty;
            pan_prefix = string.Empty;
            channel = string.Empty;

            //per tran
            amt_purchase_per_tran = 0;
            amt_cash_per_tran = 0;
            amt_payment_per_tran = 0;
            amt_transfer_per_tran = 0;

            //daily
            nr_inquiry_daily = 0;
            nr_purchase_daily = 0;
            nr_cash_daily = 0;
            nr_payment_daily = 0;
            nr_transfer_daily = 0;
            amt_purchase_daily = 0;
            amt_cash_daily = 0;
            amt_payment_daily = 0;
            amt_transfer_daily = 0;

            //weekly
            nr_inquiry_weekly = 0;
            nr_purchase_weekly = 0;
            nr_cash_weekly = 0;
            nr_payment_weekly = 0;
            nr_transfer_weekly = 0;
            amt_purchase_weekly = 0;
            amt_cash_weekly = 0;
            amt_payment_weekly = 0;
            amt_transfer_weekly = 0;

            //monthly
            nr_inquiry_monthly = 0;
            nr_purchase_monthly = 0;
            nr_cash_monthly = 0;
            nr_payment_monthly = 0;
            nr_transfer_monthly = 0;
            amt_purchase_monthly = 0;
            amt_cash_monthly = 0;
            amt_payment_monthly = 0;
            amt_transfer_monthly = 0;
        }
    }

    class CardLimit
    {
        enum ListVelocity
        {
            PerTran = 0,
            Daily = 1,
            Weekly = 2,
            Monthly = 3
        }

        private readonly CamsTranType _tran_type;
        private readonly List<CardLimitObj> _list_card_limit;

        public CardLimit()
        {
            _tran_type = new CamsTranType();
            _list_card_limit = new List<CardLimitObj>();

            //Resync();
        }

        public async Task Resync()
        {
            //clear buffer
            _list_card_limit.Clear();

            string query = "SELECT cms_products.issuer,cms_products.product,cms_products.pan_prefix,cms_products.pan_length,cms_products_limit.* FROM cms_products_limit " +
                "INNER JOIN cms_products ON cms_products.id=cms_products_limit.id_product";

            DataTable tbl = await DbMgr.getRecords(query);
            foreach (DataRow row in tbl.Rows)
            {
                CardLimitObj obj = new CardLimitObj
                {
                    issuer = row["issuer"].ToString(),
                    product = row["product"].ToString(),
                    pan_prefix = row["pan_prefix"].ToString(),
                    channel = row["channel"].ToString(),

                    amt_purchase_per_tran = NbConvert.ToLong(row["amt_purchase_per_tran"].ToString()),
                    amt_cash_per_tran = NbConvert.ToLong(row["amt_cash_per_tran"].ToString()),
                    amt_payment_per_tran = NbConvert.ToLong(row["amt_payment_per_tran"].ToString()),
                    amt_transfer_per_tran = NbConvert.ToLong(row["amt_transfer_per_tran"].ToString()),

                    nr_inquiry_daily = NbConvert.ToInt(row["nr_inquiry_daily"].ToString()),
                    nr_purchase_daily = NbConvert.ToInt(row["nr_purchase_daily"].ToString()),
                    nr_cash_daily = NbConvert.ToInt(row["nr_cash_daily"].ToString()),
                    nr_payment_daily = NbConvert.ToInt(row["nr_payment_daily"].ToString()),
                    nr_transfer_daily = NbConvert.ToInt(row["nr_transfer_daily"].ToString()),

                    amt_purchase_daily = NbConvert.ToLong(row["amt_purchase_daily"].ToString()),
                    amt_cash_daily = NbConvert.ToLong(row["amt_cash_daily"].ToString()),
                    amt_payment_daily = NbConvert.ToLong(row["amt_payment_daily"].ToString()),
                    amt_transfer_daily = NbConvert.ToLong(row["amt_transfer_daily"].ToString()),

                    nr_inquiry_weekly = NbConvert.ToInt(row["nr_inquiry_weekly"].ToString()),
                    nr_purchase_weekly = NbConvert.ToInt(row["nr_purchase_weekly"].ToString()),
                    nr_cash_weekly = NbConvert.ToInt(row["nr_cash_weekly"].ToString()),
                    nr_payment_weekly = NbConvert.ToInt(row["nr_payment_weekly"].ToString()),
                    nr_transfer_weekly = NbConvert.ToInt(row["nr_transfer_weekly"].ToString()),

                    amt_purchase_weekly = NbConvert.ToLong(row["amt_purchase_weekly"].ToString()),
                    amt_cash_weekly = NbConvert.ToLong(row["amt_cash_weekly"].ToString()),
                    amt_payment_weekly = NbConvert.ToLong(row["amt_payment_weekly"].ToString()),
                    amt_transfer_weekly = NbConvert.ToLong(row["amt_transfer_weekly"].ToString()),

                    nr_inquiry_monthly = NbConvert.ToInt(row["nr_inquiry_monthly"].ToString()),
                    nr_purchase_monthly = NbConvert.ToInt(row["nr_purchase_monthly"].ToString()),
                    nr_cash_monthly = NbConvert.ToInt(row["nr_cash_monthly"].ToString()),
                    nr_payment_monthly = NbConvert.ToInt(row["nr_payment_monthly"].ToString()),
                    nr_transfer_monthly = NbConvert.ToInt(row["nr_transfer_monthly"].ToString()),

                    amt_purchase_monthly = NbConvert.ToLong(row["amt_purchase_monthly"].ToString()),
                    amt_cash_monthly = NbConvert.ToLong(row["amt_cash_monthly"].ToString()),
                    amt_payment_monthly = NbConvert.ToLong(row["amt_payment_monthly"].ToString()),
                    amt_transfer_monthly = NbConvert.ToLong(row["amt_transfer_monthly"].ToString())
                };

                _list_card_limit.Add(obj);
            }
        }

        public async Task<long> getLimitPerTran(string issuer, string pan, string tran_type, string channel)
        {
            long ret = await getOverrideLimit(issuer, pan, tran_type, true, ListVelocity.PerTran, channel);

            if (ret == -1)
            {
                //get directly from database
                ret = await getCardLimit(issuer, pan, tran_type, true, ListVelocity.PerTran, channel);
            }

            return ret;
        }

        public async Task<long> getLimitDaily(string issuer, string pan, string tran_type, bool is_amt, string channel)
        {
            long ret = await getOverrideLimit(issuer, pan, tran_type, is_amt, ListVelocity.Daily, channel);

            if (ret == -1)
            {
                //get directly from database
                ret = await getCardLimit(issuer, pan, tran_type, is_amt, ListVelocity.Daily, channel);
            }

            return ret;
        }

        public async Task<long> getLimitWeekly(string issuer, string pan, string tran_type, bool is_amt, string channel)
        {
            long ret = await getOverrideLimit(issuer, pan, tran_type, is_amt, ListVelocity.Weekly, channel);

            if (ret == -1)
            {
                //get directly from database
                ret = await getCardLimit(issuer, pan, tran_type, is_amt, ListVelocity.Weekly, channel);
            }

            return ret;
        }

        public async Task<long> getLimitMonthly(string issuer, string pan, string tran_type, bool is_amt, string channel)
        {
            long ret = await getOverrideLimit(issuer, pan, tran_type, is_amt, ListVelocity.Monthly, channel);

            if (ret == -1)
            {
                //get directly from database
                ret = await getCardLimit(issuer, pan, tran_type, is_amt, ListVelocity.Monthly, channel);
            }

            return ret;
        }

        private async Task<long> getCardLimit(string issuer, string pan, string tran_type, bool is_amt, ListVelocity velocity, string channel)
        {
            long ret = -1;

            string query;

            if (Platform.DbServer == DbServer.Postgree)
                query = $@"SELECT cms_products.issuer,cms_products.product,
                    cms_products.pan_prefix,cms_products.pan_length,cms_products_limit.* 
                    FROM cms_products_limit
                    INNER JOIN cms_products ON cms_products.id=cms_products_limit.id_product 
                    WHERE issuer='{issuer}' AND pan_prefix='{pan.Substring(0, 6)}' AND 
                    channel='{channel}' LIMIT 1";
            else
                query = $@"SELECT TOP 1 cms_products.issuer,cms_products.product,
                    cms_products.pan_prefix,cms_products.pan_length,cms_products_limit.* 
                    FROM cms_products_limit
                    INNER JOIN cms_products ON cms_products.id=cms_products_limit.id_product 
                    WHERE issuer='{issuer}' AND pan_prefix='{pan.Substring(0, 6)}' AND 
                    channel='{channel}'";

            DataRow row = await DbMgr.getRow(query);

            switch (velocity)
            {
                case ListVelocity.PerTran:
                    switch (_tran_type.getTranType(tran_type))
                    {
                        case CamsTranType.ListTranType.Purchase:
                            ret = NbConvert.ToLong(row["amt_purchase_per_tran"].ToString());
                            break;
                        case CamsTranType.ListTranType.Cash:
                            ret = NbConvert.ToLong(row["amt_cash_per_tran"].ToString());
                            break;
                        case CamsTranType.ListTranType.Payment:
                            ret = NbConvert.ToLong(row["amt_payment_per_tran"].ToString());
                            break;
                        case CamsTranType.ListTranType.Transfer:
                            ret = NbConvert.ToLong(row["amt_transfer_per_tran"].ToString());
                            break;
                    }

                    break;
                case ListVelocity.Daily:
                    if (is_amt == false)
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Inquiry:
                                ret = NbConvert.ToLong(row["nr_inquiry_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["nr_purchase_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["nr_cash_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["nr_payment_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["nr_transfer_daily"].ToString());
                                break;
                        }
                    }
                    else
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["amt_purchase_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["amt_cash_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["amt_payment_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["amt_transfer_daily"].ToString());
                                break;
                        }
                    }

                    break;
                case ListVelocity.Weekly:
                    if (is_amt == false)
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Inquiry:
                                ret = NbConvert.ToLong(row["nr_inquiry_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["nr_purchase_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["nr_cash_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["nr_payment_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["nr_transfer_weekly"].ToString());
                                break;
                        }
                    }
                    else
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["amt_purchase_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["amt_cash_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["amt_payment_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["amt_transfer_weekly"].ToString());
                                break;
                        }
                    }

                    break;
                case ListVelocity.Monthly:
                    if (is_amt == false)
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Inquiry:
                                ret = NbConvert.ToLong(row["nr_inquiry_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["nr_purchase_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["nr_cash_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["nr_payment_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["nr_transfer_monthly"].ToString());
                                break;
                        }
                    }
                    else
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["amt_purchase_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["amt_cash_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["amt_payment_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["amt_transfer_monthly"].ToString());
                                break;
                        }
                    }

                    break;
            }

            return ret;
        }

        private async Task<long> getOverrideLimit(string issuer, string pan, string tran_type, bool is_amt, ListVelocity velocity, string channel)
        {
            long ret = -1;
            string query;

            if (Platform.DbServer == DbServer.Postgree)
                query = $@"SELECT * FROM cms_card_override_limits 
                    WHERE issuer='{issuer}' AND pan='{pan}' AND 
                    channel='{channel}' LIMIT 1";
            else
                query = $@"SELECT TOP 1 * FROM cms_card_override_limits 
                    WHERE issuer='{issuer}' AND pan='{pan}' AND channel='{channel}'";

            DataRow row = await DbMgr.getRow(query);

            switch (velocity)
            {
                case ListVelocity.PerTran:
                    switch (_tran_type.getTranType(tran_type))
                    {
                        case CamsTranType.ListTranType.Purchase:
                            ret = NbConvert.ToLong(row["amt_purchase_per_tran"].ToString());
                            break;
                        case CamsTranType.ListTranType.Cash:
                            ret = NbConvert.ToLong(row["amt_cash_per_tran"].ToString());
                            break;
                        case CamsTranType.ListTranType.Payment:
                            ret = NbConvert.ToLong(row["amt_payment_per_tran"].ToString());
                            break;
                        case CamsTranType.ListTranType.Transfer:
                            ret = NbConvert.ToLong(row["amt_transfer_per_tran"].ToString());
                            break;
                    }

                    break;
                case ListVelocity.Daily:
                    if (is_amt == false)
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Inquiry:
                                ret = NbConvert.ToLong(row["nr_inquiry_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["nr_purchase_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["nr_cash_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["nr_payment_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["nr_transfer_daily"].ToString());
                                break;
                        }
                    }
                    else
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["amt_purchase_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["amt_cash_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["amt_payment_daily"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["amt_transfer_daily"].ToString());
                                break;
                        }
                    }

                    break;
                case ListVelocity.Weekly:
                    if (is_amt == false)
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Inquiry:
                                ret = NbConvert.ToLong(row["nr_inquiry_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["nr_purchase_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["nr_cash_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["nr_payment_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["nr_transfer_weekly"].ToString());
                                break;
                        }
                    }
                    else
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["amt_purchase_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["amt_cash_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["amt_payment_weekly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["amt_transfer_weekly"].ToString());
                                break;
                        }
                    }

                    break;
                case ListVelocity.Monthly:
                    if (is_amt == false)
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Inquiry:
                                ret = NbConvert.ToLong(row["nr_inquiry_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["nr_purchase_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["nr_cash_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["nr_payment_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["nr_transfer_monthly"].ToString());
                                break;
                        }
                    }
                    else
                    {
                        switch (_tran_type.getTranType(tran_type))
                        {
                            case CamsTranType.ListTranType.Purchase:
                                ret = NbConvert.ToLong(row["amt_purchase_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Cash:
                                ret = NbConvert.ToLong(row["amt_cash_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Payment:
                                ret = NbConvert.ToLong(row["amt_payment_monthly"].ToString());
                                break;
                            case CamsTranType.ListTranType.Transfer:
                                ret = NbConvert.ToLong(row["amt_transfer_monthly"].ToString());
                                break;
                        }
                    }

                    break;
            }

            return ret;
        }
    }
}
