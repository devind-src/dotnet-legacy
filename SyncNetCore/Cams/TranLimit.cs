using SWTCoreLab.Common;
using SWTCoreLab.DbEngine;
using SWTCoreLab.Library;
using SWTCoreLab.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SWTCoreLab.Cams
{
    class TranLimit
    {
        enum ListVelocity
        {
            PerTran = 0,
            Daily = 1,
            Weekly = 2,
            Monthly = 3
        }

        private readonly CardLimit _card_limit;
        private readonly CamsTranType _tran_type;
        private readonly Issuer _issuer;
        private readonly BIN _bin;

        internal CamsTranType Tran_type => _tran_type;

        public TranLimit()
        {
            _card_limit = new CardLimit();
            _tran_type = new CamsTranType();
            _issuer = new Issuer();
            _bin = new BIN();
        }

        public async Task Resync()
        {
            await _card_limit.Resync();
            await _issuer.Resync();
            await _bin.Resync();
        }

        public async Task<string> VerifyTransaction(Message.Request req)
        {
            string ret = "00";

            try
            {
                string issuer = req.private_data.sink_node;
                string tran_type = req.tran_type;
                string pan = req.pan;

                //validate fields
                if (string.IsNullOrEmpty(pan) == true)
                {
                    MyApp.Logger("PAN is mandatory", Utils.FormatMessage(req));
                    return RespCodeCms.RC30_FORMAT_ERROR;
                }
                else if (string.IsNullOrEmpty(tran_type) == true)
                {
                    MyApp.Logger("Tran type is mandatory", Utils.FormatMessage(req));
                    return RespCodeCms.RC30_FORMAT_ERROR;
                }

                //check whether tran type supported
                //if not just bypass to the issuer, CMS will not check limitation
                if (Tran_type.IsTranTypeSupported(tran_type) == false)
                    return RespCodeCms.RC00_APPROVED;

                //BIN must be registered on CMS
                if (_bin.CheckBin(issuer, pan) == -1)
                {
                    MyApp.Logger("BIN is not registered", Utils.FormatMessage(req));
                    return RespCodeCms.RC15_NO_SUCH_ISSUER;
                }

                //do not check inqury on velocity per tran
                if (Tran_type.getTranType(tran_type) != CamsTranType.ListTranType.Inquiry)
                {
                    //check velocity per tran
                    if (_issuer.getVelocityPerTran(issuer) == 1)
                        ret = await CheckVelocityPerTran(req);
                }

                //check velocity daily
                if (ret == RespCodeCms.RC00_APPROVED)
                {
                    if (_issuer.getVelocityDaily(issuer) == 1)
                        ret = await CheckVelocityDaily(req);
                }

                //check velocity weekly
                if (ret == RespCodeCms.RC00_APPROVED)
                {
                    if (_issuer.getVelocityWeekly(issuer) == 1)
                        ret = await CheckVelocityWeekly(req);
                }

                //check velocity monthly
                if (ret == RespCodeCms.RC00_APPROVED)
                {
                    if (_issuer.getVelocityMonthly(issuer) == 1)
                        ret = await CheckVelocityMonthly(req);
                }
            }
            catch (Exception ex)
            {
                MyApp.Logger(ex.Message, ex.StackTrace);
            }

            return ret;
        }

        /*
         * this job must running everyday at 00:00 to reset all limitation checking
         */
        public async Task<int> ResetLimitation()
        {
            List<string> queries = new List<string>
            {
                @"UPDATE cms_velocity_cards SET 
                nr_inquiry_daily=0,nr_purchase_daily=0,nr_cash_daily=0,
                nr_payment_daily=0,nr_transfer_daily=0,amt_purchase_daily=0,
                amt_cash_daily=0,amt_payment_daily=0,amt_transfer_daily=0"
            };

            //reset weekly transaction
            if (DateTime.Now.DayOfWeek.ToString() == "Monday")
            {
                queries.Add(@"UPDATE cms_velocity_cards SET 
                    nr_inquiry_weekly=0,nr_purchase_weekly=0,nr_cash_weekly=0,
                    nr_payment_weekly=0,nr_transfer_weekly=0,
                    amt_purchase_weekly=0,amt_cash_weekly=0,amt_payment_weekly=0,
                    amt_transfer_weekly=0");
            }

            //reset monthly transaction
            if (DateTime.Now.Day == 1)
            {
                queries.Add(@"UPDATE cms_velocity_cards SET 
                    nr_inquiry_monthly=0,nr_purchase_monthly=0,nr_cash_monthly=0,
                    nr_payment_monthly=0,nr_transfer_monthly=0,
                    amt_purchase_monthly=0,amt_cash_monthly=0,amt_payment_monthly=0,
                    amt_transfer_monthly=0");
            }

            return await DbMgr.ExecuteAsync(queries);
        }

        private async Task<long[]> getCurrentLimit(Message.Request req, ListVelocity velocity)
        {
            long[] ret = new long[2];

            string issuer = req.private_data.sink_node;
            string channel = req.merchant_type;
            string tran_type = req.tran_type;
            string pan = req.pan;
            string query;

            if (Platform.DbServer == DbServer.Postgree)
                query = @$"SELECT * FROM cms_velocity_cards 
                    WHERE issuer='{issuer}' AND pan='{pan}' AND 
                    channel='{channel}' LIMIT 1";
            else
                query = @$"SELECT TOP 1 * FROM cms_velocity_cards 
                    WHERE issuer='{issuer}' AND pan='{pan}' AND channel='{channel}'";

            DataRow row = await DbMgr.getRow(query);

            if (row == null)
            {
                string dtnow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                //insert new record
                query = @"INSERT INTO cms_velocity_cards (issuer,pan,channel,last_update) 
                    VALUES (@issuer,@pan,@channel,@last_update)";

                object param = new { issuer = issuer, pan = pan, channel = channel, last_update = dtnow };

                if (await DbMgr.ExecuteAsync(query, param) == 0)
                {
                    ret[0] = 0;
                    ret[1] = 0;
                }
                else
                {
                    ret[0] = -1; //error
                    ret[1] = -1; //error
                }
            }
            else
            {
                switch (Tran_type.getTranType(tran_type))
                {
                    case CamsTranType.ListTranType.Inquiry:
                        switch (velocity)
                        {
                            case ListVelocity.Daily:
                                ret[0] = NbConvert.ToLong(row["nr_inquiry_daily"].ToString());
                                ret[1] = 0;
                                break;
                            case ListVelocity.Weekly:
                                ret[0] = NbConvert.ToLong(row["nr_inquiry_weekly"].ToString());
                                ret[1] = 0;
                                break;
                            case ListVelocity.Monthly:
                                ret[0] = NbConvert.ToLong(row["nr_inquiry_monthly"].ToString());
                                ret[1] = 0;
                                break;
                        }

                        break;
                    case CamsTranType.ListTranType.Purchase:
                        switch (velocity)
                        {
                            case ListVelocity.Daily:
                                ret[0] = NbConvert.ToLong(row["nr_purchase_daily"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_purchase_daily"].ToString());
                                break;
                            case ListVelocity.Weekly:
                                ret[0] = NbConvert.ToLong(row["nr_purchase_weekly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_purchase_weekly"].ToString());
                                break;
                            case ListVelocity.Monthly:
                                ret[0] = NbConvert.ToLong(row["nr_purchase_monthly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_purchase_monthly"].ToString());
                                break;
                        }
                        break;
                    case CamsTranType.ListTranType.Cash:
                        switch (velocity)
                        {
                            case ListVelocity.Daily:
                                ret[0] = NbConvert.ToLong(row["nr_cash_daily"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_cash_daily"].ToString());
                                break;
                            case ListVelocity.Weekly:
                                ret[0] = NbConvert.ToLong(row["nr_cash_weekly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_cash_weekly"].ToString());
                                break;
                            case ListVelocity.Monthly:
                                ret[0] = NbConvert.ToLong(row["nr_cash_monthly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_cash_monthly"].ToString());
                                break;
                        }
                        break;
                    case CamsTranType.ListTranType.Transfer:
                        switch (velocity)
                        {
                            case ListVelocity.Daily:
                                ret[0] = NbConvert.ToLong(row["nr_transfer_daily"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_transfer_daily"].ToString());
                                break;
                            case ListVelocity.Weekly:
                                ret[0] = NbConvert.ToLong(row["nr_transfer_weekly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_transfer_weekly"].ToString());
                                break;
                            case ListVelocity.Monthly:
                                ret[0] = NbConvert.ToLong(row["nr_transfer_monthly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_transfer_monthly"].ToString());
                                break;
                        }
                        break;
                    case CamsTranType.ListTranType.Payment:
                        switch (velocity)
                        {
                            case ListVelocity.Daily:
                                ret[0] = NbConvert.ToLong(row["nr_payment_daily"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_payment_daily"].ToString());
                                break;
                            case ListVelocity.Weekly:
                                ret[0] = NbConvert.ToLong(row["nr_payment_weekly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_payment_weekly"].ToString());
                                break;
                            case ListVelocity.Monthly:
                                ret[0] = NbConvert.ToLong(row["nr_payment_monthly"].ToString());
                                ret[1] = NbConvert.ToLong(row["amt_payment_monthly"].ToString());
                                break;
                        }
                        break;
                }
            }

            return ret;
        }

        private async Task<string> CheckVelocityPerTran(Message.Request req)
        {
            string ret = RespCodeCms.RC00_APPROVED;
            string issuer = req.private_data.sink_node;
            string tran_type = req.tran_type;
            string pan = req.pan;
            string channel = req.merchant_type;

            long amtRequest = req.amount_tran;
            long amtLimit = await _card_limit.getLimitPerTran(issuer, pan, tran_type, channel);

            if (amtRequest > amtLimit)
            {
                if (tran_type == Common.TranType.WITHDRAWAL) //cash
                {
                    MyApp.Logger("Exceed limit amount per transaction", Utils.FormatMessage(req));
                    ret = RespCodeCms.RC61_EXCEED_WDL_LIMIT;
                }
                else
                {
                    MyApp.Logger("Exceed limit amount per transaction", Utils.FormatMessage(req));
                    ret = RespCodeCms.RC13_INVALID_AMOUNT;
                }
            }

            return ret;
        }

        private async Task<string> CheckVelocityDaily(Message.Request req)
        {
            long amtRequest = req.amount_tran;

            string issuer = req.private_data.sink_node;
            string tran_type = req.tran_type;
            string pan = req.pan;
            string channel = req.merchant_type;

            //get current limit
            long[] arrRet = await getCurrentLimit(req, ListVelocity.Daily);

            //check whether insert database successfull
            if (arrRet[0] == -1 && arrRet[1] == -1)
                return RespCodeCms.RC06_ERROR;

            long currentTrxLimit = arrRet[0] + 1;
            long currentAmtLimit = arrRet[1] + amtRequest;

            DataQuery query = new DataQuery();
            string dtnow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            switch (Tran_type.getTranType(tran_type))
            {
                case CamsTranType.ListTranType.Inquiry:
                    currentAmtLimit = 0;

                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_inquiry_daily=@nr_inquiry_daily
                        WHERE issuer=@issuer AND 
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_inquiry_daily = currentTrxLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Purchase:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_purchase_daily=@nr_purchase_daily,
                        amt_purchase_daily=@amt_purchase_daily
                        WHERE issuer=@issuer AND 
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_purchase_daily = currentTrxLimit,
                        amt_purchase_daily = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Cash:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_cash_daily=@nr_cash_daily,
                        amt_cash_daily=@amt_cash_daily
                        WHERE issuer=@issuer AND 
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_cash_daily = currentTrxLimit,
                        amt_cash_daily = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Transfer:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_transfer_daily=@nr_transfer_daily,
                        amt_transfer_daily=@amt_transfer_daily
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_transfer_daily = currentTrxLimit,
                        amt_transfer_daily = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Payment:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_payment_daily=@nr_payment_daily,
                        amt_payment_daily=@amt_payment_daily
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_payment_daily = currentTrxLimit,
                        amt_payment_daily = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
            }

            string ret = "00";

            //get velocity limit
            long velocityAmtLimit = await _card_limit.getLimitDaily(issuer, pan, tran_type, true, channel);
            long velocityTrxLimit = await _card_limit.getLimitDaily(issuer, pan, tran_type, false, channel);

            //compare
            if (currentTrxLimit > velocityTrxLimit) //check trx limit
            {
                MyApp.Logger("Exceed limit daily", Utils.FormatMessage(req));
                ret = RespCodeCms.RC57_TRX_NOT_PERMITTED;
            }
            else if (Tran_type.getTranType(tran_type) != CamsTranType.ListTranType.Inquiry && (currentAmtLimit > velocityAmtLimit)) //check amt limit
            {
                MyApp.Logger("Exceed limit amount daily", Utils.FormatMessage(req));
                ret = RespCodeCms.RC13_INVALID_AMOUNT;
            }
            else
            {
                //update record
                if (await DbMgr.ExecuteAsync(query) != 0)
                    ret = RespCodeCms.RC29_FILE_UPDATE_FAILED;
            }

            return ret;
        }

        private async Task<string> CheckVelocityWeekly(Message.Request req)
        {
            long amtRequest = req.amount_tran;

            string issuer = req.private_data.sink_node;
            string channel = req.merchant_type;
            string tran_type = req.tran_type;
            string pan = req.pan;

            //get current limit
            long[] arrRet = await getCurrentLimit(req, ListVelocity.Weekly);

            //check whether insert database successfull
            if (arrRet[0] == -1 && arrRet[1] == -1)
                return RespCodeCms.RC06_ERROR;

            long currentTrxLimit = arrRet[0] + 1;
            long currentAmtLimit = arrRet[1] + amtRequest;

            DataQuery query = new DataQuery();
            string dtnow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            switch (Tran_type.getTranType(tran_type))
            {
                case CamsTranType.ListTranType.Inquiry:
                    currentAmtLimit = 0;

                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_inquiry_weekly=@nr_inquiry_weekly 
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_inquiry_weekly = currentTrxLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Purchase:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,      
                        nr_purchase_weekly=@nr_purchase_weekly, 
                        amt_purchase_weekly=@amt_purchase_weekly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_purchase_weekly = currentTrxLimit,
                        amt_purchase_weekly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Cash:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_cash_weekly=@nr_cash_weekly,
                        amt_cash_weekly=@amt_cash_weekly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_cash_weekly = currentTrxLimit,
                        amt_cash_weekly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Transfer:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_transfer_weekly=@nr_transfer_weekly,
                        amt_transfer_weekly=@amt_transfer_weekly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_transfer_weekly = currentTrxLimit,
                        amt_transfer_weekly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Payment:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_payment_weekly=@nr_payment_weekly,
                        amt_payment_weekly=@amt_payment_weekly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_payment_weekly = currentTrxLimit,
                        amt_payment_weekly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
            }

            string ret = RespCodeCms.RC00_APPROVED;

            //get velocity limit
            long velocityAmtLimit = await _card_limit.getLimitWeekly(issuer, pan, tran_type, true, channel);
            long velocityTrxLimit = await _card_limit.getLimitWeekly(issuer, pan, tran_type, false, channel);

            //compare
            if (currentTrxLimit > velocityTrxLimit) //check trx limit
            {
                MyApp.Logger("Exceed limit weekly", Utils.FormatMessage(req));
                ret = RespCodeCms.RC57_TRX_NOT_PERMITTED;
            }
            else if (Tran_type.getTranType(tran_type) != CamsTranType.ListTranType.Inquiry && (currentAmtLimit > velocityAmtLimit)) //check amt limit
            {
                MyApp.Logger("Exceed limit amount weekly", Utils.FormatMessage(req));
                ret = RespCodeCms.RC13_INVALID_AMOUNT;
            }
            else
            {
                //update record
                if (await DbMgr.ExecuteAsync(query) != 0)
                    ret = RespCodeCms.RC29_FILE_UPDATE_FAILED;
            }

            return ret;
        }

        private async Task<string> CheckVelocityMonthly(Message.Request req)
        {
            long amtRequest = req.amount_tran;

            string issuer = req.private_data.sink_node;
            string channel = req.merchant_type;
            string tran_type = req.tran_type;
            string pan = req.pan;

            //get current limit
            long[] arrRet = await getCurrentLimit(req, ListVelocity.Monthly);

            //check whether insert database successfull
            if (arrRet[0] == -1 && arrRet[1] == -1)
                return "06"; //error

            long currentTrxLimit = arrRet[0] + 1;
            long currentAmtLimit = arrRet[1] + amtRequest;

            DataQuery query = new DataQuery();
            string dtnow = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            switch (Tran_type.getTranType(tran_type))
            {
                case CamsTranType.ListTranType.Inquiry:
                    currentAmtLimit = 0;

                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,
                        nr_inquiry_monthly=@nr_inquiry_monthly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_inquiry_monthly = currentTrxLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Purchase:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,      
                        nr_purchase_monthly=@nr_purchase_monthly, 
                        amt_purchase_monthly=@amt_purchase_monthly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_purchase_monthly = currentTrxLimit,
                        amt_purchase_monthly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Cash:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,      
                        nr_cash_monthly=@nr_cash_monthly, 
                        amt_cash_monthly=@amt_cash_monthly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_cash_monthly = currentTrxLimit,
                        amt_cash_monthly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Transfer:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,      
                        nr_transfer_monthly=@nr_transfer_monthly, 
                        amt_transfer_monthly=@amt_transfer_monthly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_transfer_monthly = currentTrxLimit,
                        amt_transfer_monthly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
                case CamsTranType.ListTranType.Payment:
                    query.sqltext = $@"UPDATE cms_velocity_cards SET 
                        last_update=@last_update,      
                        nr_payment_monthly=@nr_payment_monthly, 
                        amt_payment_monthly=@amt_payment_monthly
                        WHERE issuer=@issuer AND
                        pan=@pan AND channel=@channel";

                    query.param = new
                    {
                        last_update = dtnow,
                        nr_payment_monthly = currentTrxLimit,
                        amt_payment_monthly = currentAmtLimit,
                        issuer = issuer,
                        pan = pan,
                        channel = channel
                    };

                    break;
            }

            string ret = RespCodeCms.RC00_APPROVED;

            //get velocity limit
            long velocityAmtLimit = await _card_limit.getLimitMonthly(issuer, pan, tran_type, true, channel);
            long velocityTrxLimit = await _card_limit.getLimitMonthly(issuer, pan, tran_type, false, channel);

            //compare
            if (currentTrxLimit > velocityTrxLimit) //check trx limit
            {
                MyApp.Logger("Exceed limit monthly", Utils.FormatMessage(req));
                ret = RespCodeCms.RC57_TRX_NOT_PERMITTED;
            }
            else if (Tran_type.getTranType(tran_type) != CamsTranType.ListTranType.Inquiry && (currentAmtLimit > velocityAmtLimit)) //check amt limit
            {
                MyApp.Logger("Exceed limit amount monthly", Utils.FormatMessage(req));
                ret = RespCodeCms.RC13_INVALID_AMOUNT;
            }
            else
            {
                //update record
                if (await DbMgr.ExecuteAsync(query) != 0)
                    ret = RespCodeCms.RC29_FILE_UPDATE_FAILED;
            }

            return ret;
        }
    }
}
