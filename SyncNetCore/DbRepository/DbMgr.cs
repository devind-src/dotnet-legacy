using SWTCoreLab.DbRepository;
using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Models.Common;
using SyncNet.Models.Networking;
using SyncNet.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SyncNet.DbRepository
{
    class DbMgr
    {
        private static DbService _dbService;

        internal enum EnumStatusApp
        {
            DOWN = 0,
            UP = 1
        }


        #region Service Base
        internal static async void Initialize()
        {
            _dbService = new DbService(AppConfig.ConnectionString);

            //Console.WriteLine("Connecting to db...");

            //test query
            string query = $@"SELECT command_port FROM sw_app 
                WHERE app_name = @app_name";

            //test connect to db
            string commandPort = await GetFieldValue(query, new { app_name = MyApp.APPNAME });

            if (string.IsNullOrEmpty(commandPort) == false)
                await MyApp.Logger("Connect to db successfull");
            else
                await MyApp.Logger("Connect to db failed, please check log detail");
        }

        internal static async Task<int> ExecuteAsync(string query)
        {
            var dbResult = await _dbService.ExecuteAsync(query, null);

            return dbResult.IsSuccess ? 0 : -1;
        }

        internal static async Task<int> ExecuteAsync(string sqltext, object param)
        {
            var dbResult = await _dbService.ExecuteAsync(sqltext, param);

            return dbResult.IsSuccess ? 0 : -1;
        }

        internal static async Task<int> ExecuteAsync(List<QueryModel> queries)
        {
            var commands = queries.Select(q => (q.sqltext, q.param));
            var dbResult = await _dbService.ExecuteTransactionAsync(commands);

            return dbResult.IsSuccess ? 0 : -1;
        }

        internal static async Task<string> GetFieldValue(string query)
        {
            return await _dbService.QueryFirstAsync<string>(query);
        }

        internal static async Task<string> GetFieldValue(string query, object param)
        {
            return await _dbService.QueryFirstAsync<string>(query, param);
        }


        internal static async Task<DataTable> GetRecords(string query)
        {
            return await _dbService.QueryFirstDataTableAsync(query);
        }

        internal static async Task<DataTable> GetRecords(string query, object param)
        {
            return await _dbService.QueryFirstDataTableAsync(query, param);
        }

        internal static async Task<DataRow> GetRow(string query)
        {
            return await _dbService.QueryFirstDataRowAsync(query);
        }

        internal static async Task<DataRow> GetRow(string query, object param)
        {
            return await _dbService.QueryFirstDataRowAsync(query, param);
        }

        internal static async Task<bool> IsRecordExist(string query)
        {
            return await _dbService.ExistsAsync(query);
        }

        internal static async Task<bool> IsRecordExist(string query, object param)
        {
            return await _dbService.ExistsAsync(query, param);
        }

        #endregion

        #region Get Data
        internal static async Task<bool> IsOriginalExist(Message.Request req)
        {
            //string switch_key_org = (req.original_data + req.terminal_id).ToUpper();
            string switch_key_org = DataHelper.GetSwitchKeyOrig(req.original_data, req.terminal_id);

            //check original trx
            string query = $@"SELECT tran_nr FROM sw_trans_pg 
                WHERE switch_key = @switch_key";

            return await IsRecordExist(query, new { switch_key = switch_key_org });
        }

        internal static async Task<bool> EligibleToReverse(string switch_key)
        {
            //check original transaction
            string query = $@"SELECT resp_code_rsp,tran_reversed FROM sw_trans_pg 
                WHERE switch_key = @switch_key";

            //get data
            DataRow row = await GetRow(query, new { switch_key = switch_key.ToUpper() });
            if (row == null) return false;

            string resp_code = row["resp_code_rsp"].ToString();
            string tran_reversed = row["tran_reversed"].ToString();

            //only approved, timeout & pending can be reverse
            string[] validRespCodes = { "00", "68", "0000", "1068", "1011" };
            if (string.IsNullOrEmpty(resp_code) == true) return false;
            if (!validRespCodes.Contains(resp_code)) return false;

            //transaction has been reversed
            if (string.IsNullOrEmpty(tran_reversed) == true) return false;
            if (tran_reversed == "1") return false;

            return true;
        }

        internal static async Task<int> GetAppPort()
        {
            string query = $@"SELECT command_port FROM sw_app 
                WHERE app_name = @app_name";

            return NbConvert.ToInt(await GetFieldValue(query, new { app_name = MyApp.APPNAME }));
        }

        internal static async Task<EndPointService> GetEndPointLogServices()
        {
            var ret = new EndPointService();

            string query = $"SELECT host,command_port FROM sw_app WHERE app_name = @app_name";
            DataRow rec = await GetRow(query, new { app_name = "Log Services" });

            if (rec != null)
            {
                ret.Host = rec["host"].ToString();
                ret.Port = Convert.ToInt32(rec["command_port"]);
            }

            return ret;
        }

        internal static async Task<string> GetFlagTranReversed(string switch_key)
        {
            //check original transaction
            string query = $@"SELECT tran_reversed FROM sw_trans_pg 
                WHERE switch_key = @switch_key";

            //flag tran reversed
            return await GetFieldValue(query, new { switch_key = switch_key.ToUpper() });
        }

        internal static async Task<string> GetRCReversalOriginalTransaction(string switch_key)
        {
            //check original transaction
            string query = $@"SELECT resp_code_rev FROM sw_trans_pg 
                WHERE switch_key = @switch_key";

            //flag tran reversed
            return await GetFieldValue(query, new { switch_key = switch_key.ToUpper() });
        }

        internal static async Task<string> GetAppHost(string appname)
        {
            string query = $@"SELECT host FROM sw_app 
                WHERE app_name = @app_name";

            return await GetFieldValue(query, new { app_name = appname });
        }

        internal static async Task<string> GetCmsOffset(string pan)
        {
            string query = $@"SELECT offset FROM cms_cards 
                WHERE card_number = @card_number";

            return await GetFieldValue(query, new { card_number = pan });
        }

        internal static async Task<int> GetNodeId(string NodeName)
        {
            string query = $@"SELECT node_id FROM sw_nodes 
                WHERE node_name = @node_name";

            string node_id = await GetFieldValue(query, new { node_name = NodeName });

            if (int.TryParse(node_id, out int val) == false) val = -1;
            //if (string.IsNullOrEmpty(node_id) == true) val = -1;

            return val;
        }

        internal static async Task<DataRow> GetHsmService()
        {
            string query = "SELECT * FROM sw_crypto_service";

            return await GetRow(query);
        }

        internal static async Task<string> GetHsmProtocol()
        {
            string query = "SELECT protocol FROM sw_crypto_hsm";

            return await GetFieldValue(query);
        }

        internal static async Task<DataRow> GetVaInfo(string acc_number)
        {
            string query = $@"SELECT min_balance,balance 
                FROM va_account WHERE acc_nr = @acc_nr";

            return await GetRow(query, new { acc_nr = acc_number });
        }

        internal static async Task<DataRow> GetVaInfoByMid(string mid)
        {
            string query = $@"SELECT T1.acc_nr,T1.min_balance,T1.balance 
                FROM va_account T1 JOIN sw_merchant T2 ON T2.va_name=T1.inst_name
                WHERE T2.merchant_id = @merchant_id";

            return await GetRow(query, new { merchant_id = mid });
        }

        internal static async Task<DataTable> GetHsmKeys()
        {
            string query = @"SELECT T2.node_name,T1.master_key,T1.key_under_lmk,T1.key_under_zmk,T1.key_check_value,T1.pinblock_format 
                FROM sw_crypto_keys T1 JOIN sw_nodes T2 ON T2.node_id = T1.node_id";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetHotcard()
        {
            string query = "SELECT * FROM sw_hotcard";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetNodes()
        {
            string query = @"SELECT 
                T1.node_name,
                T1.app_name,
                T1.port_in,
                T1.port_out,
                T1.inst_id,
                T1.auto_signon,
                T1.auto_reversal,
                T1.auto_reply_reversal,
                T1.keychange_timer,
                T1.echo_timer,
                T1.request_timeout,
                T1.advice_timeout,
                T1.saf_limit,
                T1.pin_translate,
                T1.provider_service,
                T1.auth_service,
                T1.issuer,
                T1.sensitive_data,
                T1.send_cutover_msg,
                T1.save_repeat_reversal,

                T2.business_calendar,
                T2.enable_closing,
                T2.time_start,
                T2.time_end 

                FROM sw_nodes T1
                JOIN sw_business_date T2 ON T2.business_calendar=T1.business_calendar";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetVaAccount()
        {
            //1 mitra, 1 rekening
            string query = $@"SELECT T1.acc_nr,T2.merchant_id,T1.min_balance,T1.balance 
                FROM va_account T1 JOIN sw_merchant T2 ON T2.va_name=T1.inst_name";

            return await GetRecords(query);
        }

        internal static async Task<ReversalStatus> GetRespCodeReversal(Message.Request req)
        {
            var m = new ReversalStatus();

            //string switch_key_org = (req.original_data + req.merchant_id).ToUpper();
            string switch_key_org = DataHelper.GetSwitchKeyOrig(req.original_data, req.terminal_id);

            string query = $@"SELECT tran_reversed,resp_code_rsp,resp_code_rev FROM sw_trans_pg 
                WHERE switch_key = @switch_key";

            DataRow row = await GetRow(query, new { switch_key = switch_key_org });
            if (row == null) return null;

            m.TranReversed = row["tran_reversed"].ToString();
            m.RespCode = row["resp_code_rsp"].ToString();
            m.RespCodeReversal = row["resp_code_rev"].ToString();

            return m;
        }

        #endregion

        #region Business Date
        internal static async Task<DataTable> GetBusinessDate()
        {
            string query = @"SELECT * FROM sw_business_date";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetPublicHoliday()
        {
            string query = @"SELECT * FROM sw_public_holiday";

            return await GetRecords(query);
        }

        internal static async Task<bool> IsPublicHoliday()
        {
            string dtnow = DateTime.Now.ToString("dd-MM-yyyy");
            string query = $@"SELECT * FROM sw_public_holiday
                WHERE holiday_date = @holiday_date";

            return await IsRecordExist(query, new { holiday_date = dtnow });
        }

        internal static async Task<int> UpdateBusinessDate(BusinessDateModel m)
        {
            string sqltext = $@"UPDATE sw_business_date SET 
                current_bsn_date = @current_bsn_date,
                previous_bsn_date = @previous_bsn_date 
                WHERE business_calendar = @business_calendar";

            var param = new
            {
                current_bsn_date = m.CurrentBusinessDate,
                previous_bsn_date = m.PreviousBusinessDate,
                business_calendar = m.CalendarName
            };

            return await ExecuteAsync(sqltext, param);
        }

        internal static async Task<int> RemovePublicHoliday()
        {
            string dtnow = DateTime.Now.ToString("dd-MM-yyyy");
            string query = $@"DELETE FROM sw_public_holiday WHERE holiday_date = @holiday_date";

            return await ExecuteAsync(query, new { holiday_date = dtnow });
        }
        #endregion

        #region Routing
        internal static async Task<DataTable> GetRouteByCluster()
        {
            string query = $@"SELECT cid,product,node_name FROM sw_cluster_member
                JOIN sw_cluster_group ON sw_cluster_group.id_group=sw_cluster_member.id_group";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetRouteByBIN()
        {
            string query = @"SELECT 
		        SN.node_name, 
		        SG.group_name AS bin_grp, 
		        SB.bin_nr 
	        FROM 
		        sw_bins SB
		        INNER JOIN sw_group SG ON SG.group_id = SB.group_id
		        INNER JOIN sw_routes_by_bin SR ON SR.group_id = SB.group_id
		        INNER JOIN sw_nodes SN ON SN.node_id = SR.node_id
	        ORDER BY 
		        CAST(SB.bin_nr AS INT) DESC";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetRouteByInst()
        {
            string query = @"SELECT 
	            SN.node_name,
	            SR.inst_id
            FROM 
	            sw_routes_by_inst SR
	            INNER JOIN sw_nodes SN ON SN.node_id = SR.node_id";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetRouteBySource()
        {
            string query = @"SELECT N1.node_name AS node_in,N2.node_name AS node_out,T1.inst_id
                FROM sw_routes_by_source T1
                JOIN sw_nodes N1 ON N1.node_id=T1.node_id_in
                JOIN sw_nodes N2 ON N2.node_id=T1.node_id_out";

            return await GetRecords(query);
        }

        internal static async Task<DataTable> GetRouteBySink()
        {
            string query = @"SELECT node_name FROM sw_nodes WHERE category IN ('0','2')";

            return await GetRecords(query);
        }
        #endregion

        #region Update Config
        internal static async Task UpdateApp(EnumStatusApp status)
        {
            string sqltext = $@"UPDATE sw_app SET status = @status,last_update = @last_update 
	            WHERE app_name = @app_name";

            var param = new
            {
                status = (int)status,
                last_update = DateTime.Now,
                app_name = MyApp.APPNAME
            };

            await ExecuteAsync(sqltext, param);
        }

        internal static async Task UpdateHsmKey(string NodeName, string KeyUnderLMK, string KeyUnderZMK, string KeyCheckValue)
        {
            int node_id = await GetNodeId(NodeName);

            string sqltext = $@"UPDATE sw_crypto_keys SET 
                key_under_lmk = @key_under_lmk,
                key_under_zmk = @key_under_zmk,
                key_check_value = @key_check_value 
                WHERE node_id = @node_id";

            var param = new
            {
                key_under_lmk = KeyUnderLMK,
                key_under_zmk = KeyUnderZMK,
                key_check_value = KeyCheckValue,
                node_id = node_id
            };

            await ExecuteAsync(sqltext, param);
        }
        #endregion

        #region Insert Update Transaction
        internal static async Task<int> InsertTrx(Message.Request req)
        {
            string switch_key = DataHelper.GetSwitchKey(req);
            string additional_data = DataHelper.GetAdditionalData(req.additional_data);
            string original_data = DataHelper.GetOrigData(req.original_data);
            string tran_reversed = "0";

            //validate amount va
            if (req.virtual_account.enable == true && req.virtual_account.amount == 0)
                req.virtual_account.amount = req.amount_tran;

            //sensitive data
            string pan = req.pan;
            string track2data = req.security.track2data;
            string pan_encrypted = string.Empty;
            string track2data_encrypted = string.Empty;

            if (req.security.protect_sensitive_data == "1")
            {
                string key = req.trace_number.PadRight(16, 'F');

                pan_encrypted = NbPCIDSS.EncryptMessage(pan, key);
                track2data_encrypted = NbPCIDSS.EncryptMessage(track2data, key);

                pan = string.Empty;
                track2data = string.Empty;
            }

            var queries = new List<QueryModel>();
            var q1 = new QueryModel();
            int ret = 0;

            //debet virtual account
            if (req.virtual_account.enable == true)
            {
                switch (req.tran_type)
                {
                    case TranType.WITHDRAWAL:
                    case TranType.DEPOSIT:
                    case TranType.PAYMENT:
                    case TranType.PURCHASE:
                    case TranType.TRANSFER:
                        VaResult obj = await VirtualAccountService.Debet(req);
                        ret = obj.ret;

                        //add query va
                        if (string.IsNullOrEmpty(obj.query.sqltext) == false)
                            queries.Add(obj.query);

                        break;
                }
            }

            q1.sqltext =
                $@"INSERT INTO sw_trans_pg (switch_key,source_node,dest_node,state,
                pan,msgtype,tran_type,tran_type_ext,from_acc_type,to_acc_type,amount_tran_req,amount_va,
                fee_total,fee_acq,fee_swt,fee_bil,fee_iss,fee_mer,fee_sub,tran_datetime,date_settle_req,
                trace_number,acq_inst_id,fwd_inst_id,track2data,reff_number,merchant_type,pos_entry_mode,
                terminal_id,merchant_id,currency,last_balance,receiving_inst_id,from_acc_number,to_acc_number,va_acc_number,
                orig_data,source_tran,auth_tran,tran_reversed,node_data_req,pan_encrypted,track2data_encrypted,
                icc_data,ip_endpoint,time_req) 
                VALUES (@switch_key,@source_node,@dest_node,@state,
                @pan,@msgtype,@tran_type,@tran_type_ext,@from_acc_type,@to_acc_type,@amount_tran_req,@amount_va,
                @fee_total,@fee_acq,@fee_swt,@fee_bil,@fee_iss,@fee_mer,@fee_sub,@tran_datetime,@date_settle_req,
                @trace_number,@acq_inst_id,@fwd_inst_id,@track2data,@reff_number,@merchant_type,@pos_entry_mode,
                @terminal_id,@merchant_id,@currency,@last_balance,@receiving_inst_id,@from_acc_number,@to_acc_number,@va_acc_number,
                @orig_data,@source_tran,@auth_tran,@tran_reversed,@node_data_req,@pan_encrypted,@track2data_encrypted,
                @icc_data,@ip_endpoint,@time_req)";

            q1.param = new
            {
                switch_key = switch_key,
                source_node = req.private_data.source_node,
                dest_node = req.private_data.sink_node,
                state = State.PROCESS,
                pan = pan,
                msgtype = req.msgtype,
                tran_type = req.tran_type.ToUpper(),
                tran_type_ext = req.tran_type_ext,
                from_acc_type = req.from_acc_type,
                to_acc_type = req.to_acc_type,
                amount_tran_req = req.amount_tran,
                amount_va = req.virtual_account.amount,
                fee_total = req.fee_data.total_fee,
                fee_acq = req.fee_data.acquirer_fee,
                fee_swt = req.fee_data.switch_fee,
                fee_bil = req.fee_data.biller_fee,
                fee_iss = req.fee_data.issuer_fee,
                fee_mer = req.fee_data.merchant_fee,
                fee_sub = req.fee_data.submerchant_fee,
                tran_datetime = req.datetime_tran,
                date_settle_req = req.date_settle,
                trace_number = req.trace_number,
                acq_inst_id = req.acq_inst_id,
                fwd_inst_id = req.fwd_inst_id,
                track2data = track2data,
                pos_entry_mode = req.pos_entry_mode,
                reff_number = req.refnum,
                merchant_type = req.merchant_type,
                terminal_id = req.terminal_id,
                merchant_id = req.merchant_id,
                currency = req.currency,
                last_balance = req.virtual_account.balance,
                receiving_inst_id = req.receiving_inst_id,
                from_acc_number = req.from_acc_number,
                to_acc_number = req.to_acc_number,
                va_acc_number = req.virtual_account.acc_number,
                orig_data = original_data,
                source_tran = SourceTran.EXTERNAL,
                auth_tran = AuthTran.EXTERNAL,
                tran_reversed = tran_reversed,
                node_data_req = additional_data,
                pan_encrypted = pan_encrypted,
                track2data_encrypted = track2data_encrypted,
                icc_data = req.security.iccdata,
                ip_endpoint = req.private_data.ip_external,
                time_req = DateTime.Now
            };

            //set queries
            queries.Add(q1);

            //debet va success, insert tran
            if (ret == 0) ret = await ExecuteAsync(queries);

            return ret;
        }

        internal static async Task<int> InsertTrxAdvRev(Message.Request req)
        {
            string switch_key = DataHelper.GetSwitchKey(req);
            string additional_data = DataHelper.GetAdditionalData(req.additional_data);
            string original_data = DataHelper.GetOrigData(req.original_data);
            string tran_reversed = "0";

            //validate amount va
            if (req.virtual_account.enable == true && req.virtual_account.amount == 0)
                req.virtual_account.amount = req.amount_tran;

            //sensitive data
            string pan = req.pan;
            string track2data = req.security.track2data;
            string pan_encrypted = string.Empty;
            string track2data_encrypted = string.Empty;

            if (req.security.protect_sensitive_data == "1")
            {
                string key = req.trace_number.PadRight(16, 'F');

                pan_encrypted = NbPCIDSS.EncryptMessage(pan, key);
                track2data_encrypted = NbPCIDSS.EncryptMessage(track2data, key);

                pan = string.Empty;
                track2data = string.Empty;
            }

            //string switch_key_org = (req.original_data + req.merchant_id).ToUpper();
            string switch_key_org = DataHelper.GetSwitchKeyOrig(req.original_data, req.terminal_id);
            string orig_data_rev = req.tran_type.ToUpper() + req.datetime_tran + req.trace_number;

            //update orig trx 
            QueryModel q1 = new QueryModel();
            q1.sqltext = $@"UPDATE sw_trans_pg SET orig_data = @orig_data
                WHERE switch_key = @switch_key";

            q1.param = new { orig_data = orig_data_rev, switch_key = switch_key_org };

            QueryModel q2 = new QueryModel();
            q2.sqltext = $@"INSERT INTO sw_trans_pg (switch_key,source_node,dest_node,state,
                pan,msgtype,tran_type,tran_type_ext,from_acc_type,to_acc_type,amount_tran_req,amount_va,
                fee_total,fee_acq,fee_swt,fee_bil,fee_iss,fee_mer,fee_sub,tran_datetime,date_settle_req,
                trace_number,acq_inst_id,fwd_inst_id,track2data,reff_number,merchant_type,pos_entry_mode,
                terminal_id,merchant_id,currency,last_balance,receiving_inst_id,from_acc_number,to_acc_number,va_acc_number,
                orig_data,source_tran,auth_tran,tran_reversed,node_data_req,pan_encrypted,track2data_encrypted,
                icc_data,ip_endpoint,time_req) 
                VALUES (@switch_key,@source_node,@dest_node,@state,
                @pan,@msgtype,@tran_type,@tran_type_ext,@from_acc_type,@to_acc_type,@amount_tran_req,@amount_va,
                @fee_total,@fee_acq,@fee_swt,@fee_bil,@fee_iss,@fee_mer,@fee_sub,@tran_datetime,@date_settle_req,
                @trace_number,@acq_inst_id,@fwd_inst_id,@track2data,@reff_number,@merchant_type,@pos_entry_mode,
                @terminal_id,@merchant_id,@currency,@last_balance,@receiving_inst_id,@from_acc_number,@to_acc_number,@va_acc_number,
                @orig_data,@source_tran,@auth_tran,@tran_reversed,@node_data_req,@pan_encrypted,@track2data_encrypted,
                @icc_data,@ip_endpoint,@time_req)";

            q2.param = new
            {
                switch_key = switch_key,
                source_node = req.private_data.source_node,
                dest_node = req.private_data.sink_node,
                state = State.PROCESS,
                pan = pan,
                msgtype = req.msgtype,
                tran_type = req.tran_type.ToUpper(),
                tran_type_ext = req.tran_type_ext,
                from_acc_type = req.from_acc_type,
                to_acc_type = req.to_acc_type,
                amount_tran_req = req.amount_tran,
                amount_va = req.virtual_account.amount,
                fee_total = req.fee_data.total_fee,
                fee_acq = req.fee_data.acquirer_fee,
                fee_swt = req.fee_data.switch_fee,
                fee_bil = req.fee_data.biller_fee,
                fee_iss = req.fee_data.issuer_fee,
                fee_mer = req.fee_data.merchant_fee,
                fee_sub = req.fee_data.submerchant_fee,
                tran_datetime = req.datetime_tran,
                date_settle_req = req.date_settle,
                trace_number = req.trace_number,
                acq_inst_id = req.acq_inst_id,
                fwd_inst_id = req.fwd_inst_id,
                track2data = track2data,
                pos_entry_mode = req.pos_entry_mode,
                reff_number = req.refnum,
                merchant_type = req.merchant_type,
                terminal_id = req.terminal_id,
                merchant_id = req.merchant_id,
                currency = req.currency,
                last_balance = req.virtual_account.balance,
                receiving_inst_id = req.receiving_inst_id,
                from_acc_number = req.from_acc_number,
                to_acc_number = req.to_acc_number,
                va_acc_number = req.virtual_account.acc_number,
                orig_data = original_data,
                source_tran = SourceTran.INTERNAL,
                auth_tran = AuthTran.INTERNAL,
                tran_reversed = tran_reversed,
                node_data_req = additional_data,
                pan_encrypted = pan_encrypted,
                track2data_encrypted = track2data_encrypted,
                icc_data = req.security.iccdata,
                ip_endpoint = req.private_data.ip_external,
                time_req = DateTime.Now
            };

            //set query
            List<QueryModel> queries = new List<QueryModel>();
            queries.Add(q1);
            queries.Add(q2);

            return await ExecuteAsync(queries);
        }

        internal static async Task<int> InsertTrxAuthInternal(Message.Response rsp)
        {
            DateTime dtnow = DateTime.Now;

            string switch_key = DataHelper.GetSwitchKey(rsp);
            string additional_data = DataHelper.GetAdditionalData(rsp.additional_data);
            string original_data = DataHelper.GetOrigData(rsp.original_data);
            string tran_reversed = "0";

            //validate amount va
            if (rsp.virtual_account.enable == true && rsp.virtual_account.amount == 0)
                rsp.virtual_account.amount = rsp.amount_tran;

            //sensitive data
            string pan = rsp.pan;
            string track2data = rsp.security.track2data;
            string pan_encrypted = string.Empty;
            string track2data_encrypted = string.Empty;

            if (rsp.security.protect_sensitive_data == "1")
            {
                string key = rsp.trace_number.PadRight(16, 'F');

                pan_encrypted = NbPCIDSS.EncryptMessage(pan, key);
                track2data_encrypted = NbPCIDSS.EncryptMessage(track2data, key);

                pan = string.Empty;
                track2data = string.Empty;
            }

            string sqltext =
                $@"INSERT INTO sw_trans_pg (switch_key,source_node,dest_node,state,
                pan,msgtype,tran_type,tran_type_ext,from_acc_type,to_acc_type,amount_tran_req,amount_va,
                fee_total,fee_acq,fee_swt,fee_bil,fee_iss,fee_mer,fee_sub,tran_datetime,date_settle_req,
                trace_number,acq_inst_id,fwd_inst_id,track2data,reff_number,merchant_type,pos_entry_mode,
                terminal_id,merchant_id,currency,last_balance,receiving_inst_id,from_acc_number,
                to_acc_number,va_acc_number,orig_data,auth_tran,tran_reversed,node_data_req,pan_encrypted,
                track2data_encrypted,resp_code_rsp,icc_data,ip_endpoint,time_req,time_rsp) 
                VALUES (@switch_key,@source_node,@dest_node,@state,
                @pan,@msgtype,@tran_type,@tran_type_ext,@from_acc_type,@to_acc_type,@amount_tran_req,@amount_va,
                @fee_total,@fee_acq,@fee_swt,@fee_bil,@fee_iss,@fee_mer,@fee_sub,@tran_datetime,@date_settle_req,
                @trace_number,@acq_inst_id,@fwd_inst_id,@track2data,@reff_number,@merchant_type,@pos_entry_mode,
                @terminal_id,@merchant_id,@currency,@last_balance,@receiving_inst_id,@from_acc_number,
                @to_acc_number,@va_acc_number,@orig_data,@auth_tran,@tran_reversed,@node_data_req,@pan_encrypted,
                @track2data_encrypted,@resp_code_rsp,@icc_data,@ip_endpoint,@time_req,@time_rsp)";

            var param = new
            {
                switch_key = switch_key,
                source_node = rsp.private_data.source_node,
                dest_node = rsp.private_data.sink_node,
                state = State.DONE,
                pan = pan,
                msgtype = rsp.msgtype,
                tran_type = rsp.tran_type.ToUpper(),
                tran_type_ext = rsp.tran_type_ext,
                from_acc_type = rsp.from_acc_type,
                to_acc_type = rsp.to_acc_type,
                amount_tran_req = rsp.amount_tran,
                amount_va = rsp.virtual_account.amount,
                fee_total = rsp.fee_data.total_fee,
                fee_acq = rsp.fee_data.acquirer_fee,
                fee_swt = rsp.fee_data.switch_fee,
                fee_bil = rsp.fee_data.biller_fee,
                fee_iss = rsp.fee_data.issuer_fee,
                fee_mer = rsp.fee_data.merchant_fee,
                fee_sub = rsp.fee_data.submerchant_fee,
                tran_datetime = rsp.datetime_tran,
                date_settle_req = rsp.date_settle,
                trace_number = rsp.trace_number,
                acq_inst_id = rsp.acq_inst_id,
                fwd_inst_id = rsp.fwd_inst_id,
                track2data = track2data,
                pos_entry_mode = rsp.pos_entry_mode,
                reff_number = rsp.refnum,
                merchant_type = rsp.merchant_type,
                terminal_id = rsp.terminal_id,
                merchant_id = rsp.merchant_id,
                currency = rsp.currency,
                last_balance = rsp.virtual_account.balance,
                receiving_inst_id = rsp.receiving_inst_id,
                from_acc_number = rsp.from_acc_number,
                to_acc_number = rsp.to_acc_number,
                va_acc_number = rsp.virtual_account.acc_number,
                orig_data = original_data,
                auth_tran = AuthTran.INTERNAL,
                tran_reversed = tran_reversed,
                node_data_req = additional_data,
                pan_encrypted = pan_encrypted,
                track2data_encrypted = track2data_encrypted,
                resp_code_rsp = rsp.resp_code,
                icc_data = rsp.security.iccdata,
                ip_endpoint = rsp.private_data.ip_external,
                time_req = dtnow,
                time_rsp = dtnow
            };

            //execute
            return await ExecuteAsync(sqltext, param);
        }

        internal static async Task<Message.Response> VirtualAccount(Message.Request req)
        {
            DateTime dtreq = DateTime.Now;
            List<QueryModel> queries = new List<QueryModel>();

            //init object
            Message.Response rsp = new Message.Response(req);

            VaResult obj = new VaResult();

            if (req.tran_type == TranType.VTOPUP)
                obj = await Services.VirtualAccountService.Topup(rsp);
            else if (req.tran_type == TranType.VADJUST)
                obj = await Services.VirtualAccountService.Adjust(rsp);
            else if (req.tran_type == TranType.VBALANCE)
                obj = await Services.VirtualAccountService.getBalance(rsp);

            //add query va
            if (string.IsNullOrEmpty(obj.query.sqltext) == false)
                queries.Add(obj.query);

            //set va result
            int ret = obj.ret;

            //set response for db
            switch (ret)
            {
                case 0:
                    rsp.resp_code = "0000";
                    rsp.resp_message = "Transaction success";
                    break;
                case -50:
                    rsp.resp_code = RespCodeOct.RC50_VA_NOT_FOUND;
                    rsp.resp_message = "Virtual account not found";
                    rsp.virtual_account.balance = 0;
                    break;
                case -51:
                    rsp.resp_code = RespCodeOct.RC51_INSUFICIENT_FUND;
                    rsp.resp_message = "Insuficient fund";
                    rsp.virtual_account.balance = 0;
                    break;
                default:
                    rsp.resp_code = RespCodeOct.RC06_TRAN_FAILED;
                    rsp.resp_message = "Transaction Failed";
                    rsp.virtual_account.balance = 0;
                    break;
            }

            //jika cek saldo VA tdk perlu insert trx
            //if (req.tran_type == TranType.VBALANCE) return rsp;

            DateTime dtrsp = DateTime.Now;

            string switch_key = DataHelper.GetSwitchKey(req);
            string additional_data_req = DataHelper.GetAdditionalData(req.additional_data);
            string additional_data_rsp = "";
            string tran_reversed = "0";

            //validate amount va
            if (req.virtual_account.enable == true && req.virtual_account.amount == 0)
                req.virtual_account.amount = req.amount_tran;

            QueryModel q2 = new QueryModel();
            q2.sqltext =
                $@"INSERT INTO sw_trans_pg (switch_key,source_node,dest_node,state,
                pan,msgtype,tran_type,tran_type_ext,from_acc_type,to_acc_type,amount_tran_req,amount_tran_rsp,amount_va,
                fee_total,fee_acq,fee_swt,fee_bil,fee_iss,fee_mer,fee_sub,tran_datetime,date_settle_req,
                date_settle_rsp,trace_number,acq_inst_id,fwd_inst_id,track2data,reff_number,
                merchant_type,terminal_id,merchant_id,currency,last_balance,receiving_inst_id,
                from_acc_number,to_acc_number,va_acc_number,orig_data,source_tran,auth_tran,node_data_req,node_data_rsp,
                resp_code_rsp,additional_amount,tran_reversed,icc_data,ip_endpoint,time_req,time_rsp) 
                VALUES (@switch_key,@source_node,@dest_node,@state,
                @pan,@msgtype,@tran_type,@tran_type_ext,@from_acc_type,@to_acc_type,@amount_tran_req,@amount_tran_rsp,@amount_va,
                @fee_total,@fee_acq,@fee_swt,@fee_bil,@fee_iss,@fee_mer,@fee_sub,@tran_datetime,@date_settle_req,
                @date_settle_rsp,@trace_number,@acq_inst_id,@fwd_inst_id,@track2data,@reff_number,
                @merchant_type,@terminal_id,@merchant_id,@currency,@last_balance,@receiving_inst_id,
                @from_acc_number,@to_acc_number,@va_acc_number,@orig_data,@source_tran,@auth_tran,@node_data_req,@node_data_rsp,
                @resp_code_rsp,@additional_amount,@tran_reversed,@icc_data,@ip_endpoint,@time_req,@time_rsp)";

            q2.param = new
            {
                switch_key = switch_key,
                source_node = rsp.private_data.source_node,
                dest_node = "VirtualAccount",
                state = State.DONE,
                pan = rsp.pan,
                msgtype = rsp.msgtype,
                tran_type = rsp.tran_type.ToUpper(),
                tran_type_ext = rsp.tran_type_ext,
                from_acc_type = rsp.from_acc_type,
                to_acc_type = rsp.to_acc_type,
                amount_tran_req = rsp.amount_tran,
                amount_tran_rsp = rsp.amount_tran,
                amount_va = rsp.virtual_account.amount,
                fee_total = rsp.fee_data.total_fee,
                fee_acq = rsp.fee_data.acquirer_fee,
                fee_swt = rsp.fee_data.switch_fee,
                fee_bil = rsp.fee_data.biller_fee,
                fee_iss = rsp.fee_data.issuer_fee,
                fee_mer = rsp.fee_data.merchant_fee,
                fee_sub = rsp.fee_data.submerchant_fee,
                tran_datetime = rsp.datetime_tran,
                date_settle_req = rsp.date_settle,
                date_settle_rsp = rsp.date_settle,
                trace_number = rsp.trace_number,
                acq_inst_id = rsp.acq_inst_id,
                fwd_inst_id = rsp.fwd_inst_id,
                track2data = rsp.security.track2data,
                reff_number = rsp.refnum,
                merchant_type = rsp.merchant_type,
                terminal_id = rsp.terminal_id,
                merchant_id = rsp.merchant_id,
                currency = rsp.currency,
                last_balance = rsp.virtual_account.balance,
                receiving_inst_id = rsp.receiving_inst_id,
                from_acc_number = rsp.from_acc_number,
                to_acc_number = rsp.to_acc_number,
                va_acc_number = rsp.virtual_account.acc_number,
                orig_data = rsp.original_data,
                source_tran = SourceTran.EXTERNAL,
                auth_tran = AuthTran.INTERNAL,
                node_data_req = additional_data_req,
                node_data_rsp = additional_data_rsp,
                resp_code_rsp = rsp.resp_code,
                additional_amount = rsp.additional_amount,
                tran_reversed = tran_reversed,
                icc_data = rsp.security.iccdata,
                ip_endpoint = rsp.private_data.ip_external,
                time_req = dtreq,
                time_rsp = dtrsp
            };

            queries.Add(q2);

            //execute
            ret = await ExecuteAsync(queries);
            if (ret != 0)
            {
                rsp.resp_code = RespCodeOct.RC06_TRAN_FAILED;
                rsp.resp_message = "Transaction Failed";
                rsp.virtual_account.balance = 0;
            }

            return rsp;
        }

        internal static async Task<int> UpdateTrx(Message.Response rsp)
        {
            int ret;

            switch (rsp.tran_type)
            {
                case TranType.WITHDRAWAL:
                case TranType.DEPOSIT:
                case TranType.PAYMENT:
                case TranType.PURCHASE:
                case TranType.TRANSFER:
                    ret = await UpdateTrxFinancial(rsp);
                    break;
                case TranType.REFUND:
                    ret = await UpdateTrxRefund(rsp);
                    break;
                case TranType.ADVICE:
                    ret = await UpdateTrxAdvice(rsp);
                    break;
                case TranType.REVERSAL:
                    ret = await UpdateTrxReversal(rsp);
                    break;
                default:
                    ret = await UpdateTrxOther(rsp);
                    break;
            }

            return ret;
        }

        private static async Task<int> UpdateTrxFinancial(Message.Response rsp)
        {
            string switch_key = DataHelper.GetSwitchKey(rsp);
            string auth_tran = rsp.authorized_by == AuthTran.EXTERNAL ? "1" : "0";
            string additional_data = DataHelper.GetAdditionalData(rsp.additional_data);

            QueryModel q1 = new QueryModel();
            q1.sqltext =
                $@"UPDATE sw_trans_pg SET
                    node_data_rsp = @node_data_rsp,
                    from_acc_number = @from_acc_number,
                    amount_tran_rsp = @amount_tran_rsp,
                    additional_amount = @additional_amount,
                    date_settle_rsp = @date_settle_rsp,
                    resp_code_rsp = @resp_code_rsp,
                    auth_tran = @auth_tran,
                    time_rsp = @time_rsp,
                    state = @state 
                WHERE switch_key = @switch_key";

            q1.param = new
            {
                node_data_rsp = additional_data,
                from_acc_number = rsp.from_acc_number,
                amount_tran_rsp = rsp.amount_tran,
                additional_amount = rsp.additional_amount,
                date_settle_rsp = rsp.date_settle,
                resp_code_rsp = rsp.resp_code,
                auth_tran = auth_tran,
                time_rsp = DateTime.Now,
                state = State.DONE,
                switch_key = switch_key
            };

            List<QueryModel> queries = new List<QueryModel>();
            queries.Add(q1);

            if (rsp.virtual_account.enable == true)
            {
                switch (rsp.resp_code)
                {
                    //rc 2 digit
                    case RespCodeHost.RC00_SUCCESS://success
                    case RespCodeHost.RC68_TIMEOUT://timeout

                    //rc 4 digit
                    case RespCodeOct.RC00_SUCCESS://success
                    case RespCodeOct.RC11_TRAN_PENDING://pending
                    case RespCodeOct.RC68_TIMEOUT://timeout

                        //do not reverse
                        break;

                    //trx decline, reverse balance
                    default:
                        VaResult obj = await Services.VirtualAccountService.Credit(rsp);

                        //add query va 
                        if (string.IsNullOrEmpty(obj.query.sqltext) == false)
                            queries.Add(obj.query);

                        break;
                }
            }

            return await ExecuteAsync(queries);
        }

        private static async Task<int> UpdateTrxReversal(Message.Response rsp)
        {
            //string switch_key_org = (rsp.original_data + rsp.merchant_id).ToUpper();
            string switch_key_org = DataHelper.GetSwitchKeyOrig(rsp.original_data, rsp.terminal_id);
            string switch_key = DataHelper.GetSwitchKey(rsp);
            string orig_data_rev = rsp.tran_type.ToUpper() + rsp.datetime_tran + rsp.trace_number;
            string auth_tran = rsp.authorized_by == AuthTran.EXTERNAL ? "1" : "0";
            string additional_data = DataHelper.GetAdditionalData(rsp.additional_data);

            //get rc reversal original transaction
            string rc = await GetRCReversalOriginalTransaction(switch_key_org);

            List<QueryModel> queries = new List<QueryModel>();

            //kalau reversal sebelumnya sdh berhasil jangan di update lagi
            if (rc != RespCodeHost.RC00_SUCCESS)
            {
                QueryModel q1 = new QueryModel();

                //update orig trx
                q1.sqltext = $@"UPDATE sw_trans_pg SET
                    resp_code_rev = @resp_code_rev,
                    orig_data = @orig_data,
                    tran_reversed = '1' 
                    WHERE switch_key = @switch_key";

                q1.param = new
                {
                    resp_code_rev = rsp.resp_code,
                    orig_data = orig_data_rev,
                    switch_key = switch_key_org
                };

                //add query update orig tran
                queries.Add(q1);
            }

            //virtual account reverse balance
            if (rsp.virtual_account.enable == true && rsp.resp_code == RespCodeHost.RC00_SUCCESS)
            {
                VaResult obj = await Services.VirtualAccountService.Reverse(rsp);

                //add query va
                if (string.IsNullOrEmpty(obj.query.sqltext) == false)
                    queries.Add(obj.query);
            }

            QueryModel q2 = new QueryModel();

            //update reversal trx
            q2.sqltext = $@"UPDATE sw_trans_pg SET
                node_data_rsp = @node_data_rsp,
                from_acc_number = @from_acc_number,
                amount_tran_rsp = @amount_tran_rsp,
                additional_amount = @additional_amount,
                date_settle_rsp = @date_settle_rsp,
                resp_code_rsp = @resp_code_rsp,
                auth_tran = @auth_tran,
                time_rsp = @time_rsp,
                state = @state 
                WHERE switch_key = @switch_key";

            q2.param = new
            {
                node_data_rsp = additional_data,
                from_acc_number = rsp.from_acc_number,
                amount_tran_rsp = rsp.amount_tran,
                additional_amount = rsp.additional_amount,
                date_settle_rsp = rsp.date_settle,
                resp_code_rsp = rsp.resp_code,
                auth_tran = auth_tran,
                time_rsp = DateTime.Now,
                state = State.DONE,
                switch_key = switch_key
            };

            //add query update reversal
            queries.Add(q2);

            return await ExecuteAsync(queries);
        }

        private static async Task<int> UpdateTrxAdvice(Message.Response rsp)
        {
            //string switch_key_org = (rsp.original_data + rsp.merchant_id).ToUpper();
            string switch_key_org = DataHelper.GetSwitchKeyOrig(rsp.original_data, rsp.terminal_id);
            string switch_key = DataHelper.GetSwitchKey(rsp);
            string orig_data_adv = rsp.tran_type.ToUpper() + rsp.datetime_tran + rsp.trace_number;
            string auth_tran = rsp.authorized_by == AuthTran.EXTERNAL ? "1" : "0";
            string additional_data = DataHelper.GetAdditionalData(rsp.additional_data);

            //update orig trx 
            QueryModel q1 = new QueryModel();
            q1.sqltext = $@"UPDATE sw_trans_pg SET
                resp_code_adv = @resp_code_adv 
                WHERE switch_key = @switch_key";

            q1.param = new
            {
                resp_code_adv = rsp.resp_code,
                //orig_data = orig_data_adv,
                switch_key = switch_key_org
            };

            //update advice trx
            QueryModel q2 = new QueryModel();
            q2.sqltext = $@"UPDATE sw_trans_pg SET
                node_data_rsp = @node_data_rsp,
                from_acc_number = @from_acc_number,
                amount_tran_rsp = @amount_tran_rsp,
                additional_amount = @additional_amount,
                date_settle_rsp = @date_settle_rsp,
                resp_code_rsp = @resp_code_rsp,
                auth_tran = @auth_tran,
                time_rsp = @time_rsp,
                state = @state 
                WHERE switch_key = @switch_key";

            //update advice trx
            q2.param = new
            {
                node_data_rsp = additional_data,
                from_acc_number = rsp.from_acc_number,
                amount_tran_rsp = rsp.amount_tran,
                additional_amount = rsp.additional_amount,
                date_settle_rsp = rsp.date_settle,
                resp_code_rsp = rsp.resp_code,
                auth_tran = auth_tran,
                time_rsp = DateTime.Now,
                state = State.DONE,
                switch_key = switch_key
            };

            //set queries
            List<QueryModel> queries = new List<QueryModel>();
            queries.Add(q1);
            queries.Add(q2);

            return await ExecuteAsync(queries);
        }

        private static async Task<int> UpdateTrxRefund(Message.Response rsp)
        {
            //string switch_key_org = (rsp.original_data + rsp.merchant_id).ToUpper();
            string switch_key_org = DataHelper.GetSwitchKeyOrig(rsp.original_data, rsp.terminal_id);
            string switch_key = DataHelper.GetSwitchKey(rsp);
            string orig_data_refund = rsp.tran_type.ToUpper() + rsp.datetime_tran + rsp.trace_number;
            string auth_tran = rsp.authorized_by == AuthTran.EXTERNAL ? "1" : "0";
            string additional_data = DataHelper.GetAdditionalData(rsp.additional_data);

            //update orig trx 
            QueryModel q1 = new QueryModel();
            q1.sqltext = $@"UPDATE sw_trans_pg SET
                resp_code_rev = @resp_code_rev,
                orig_data = @orig_data,
                tran_reversed = '1' 
                WHERE switch_key = @switch_key";

            q1.param = new
            {
                resp_code_rev = rsp.resp_code,
                orig_data = orig_data_refund,
                switch_key = switch_key_org
            };

            //update refund trx
            QueryModel q2 = new QueryModel();
            q2.sqltext = $@"UPDATE sw_trans_pg SET
                node_data_rsp = @node_data_rsp,
                from_acc_number = @from_acc_number,
                amount_tran_rsp = @amount_tran_rsp,
                additional_amount = @additional_amount,
                date_settle_rsp = @date_settle_rsp,
                resp_code_rsp = @resp_code_rsp,
                auth_tran = @auth_tran,
                time_rsp = @time_rsp,
                state = @state 
                WHERE switch_key = @switch_key";

            q2.param = new
            {
                node_data_rsp = additional_data,
                from_acc_number = rsp.from_acc_number,
                amount_tran_rsp = rsp.amount_tran,
                additional_amount = rsp.additional_amount,
                date_settle_rsp = rsp.date_settle,
                resp_code_rsp = rsp.resp_code,
                auth_tran = auth_tran,
                time_rsp = DateTime.Now,
                state = State.DONE,
                switch_key = switch_key
            };

            //set queries
            List<QueryModel> queries = new List<QueryModel>();
            queries.Add(q1);
            queries.Add(q2);

            //reverse balance
            if (rsp.virtual_account.enable == true && rsp.resp_code == RespCodeHost.RC00_SUCCESS)
            {
                VaResult obj = await Services.VirtualAccountService.Reverse(rsp);

                //add query va
                if (string.IsNullOrEmpty(obj.query.sqltext) == false)
                    queries.Add(obj.query);
            }

            return await ExecuteAsync(queries);
        }

        private static async Task<int> UpdateTrxOther(Message.Response rsp)
        {
            string switch_key = DataHelper.GetSwitchKey(rsp);
            string auth_tran = rsp.authorized_by == AuthTran.EXTERNAL ? "1" : "0";
            string additional_data = DataHelper.GetAdditionalData(rsp.additional_data);

            string sqltext =
                $@"UPDATE sw_trans_pg SET
                node_data_rsp = @node_data_rsp,
                from_acc_number = @from_acc_number,
                amount_tran_rsp = @amount_tran_rsp,
                additional_amount = @additional_amount,
                date_settle_rsp = @date_settle_rsp,
                resp_code_rsp = @resp_code_rsp,
                time_rsp = @time_rsp,
                auth_tran = @auth_tran,
                state = @state 
                WHERE switch_key = @switch_key";

            var param = new
            {
                node_data_rsp = additional_data,
                from_acc_number = rsp.from_acc_number,
                amount_tran_rsp = rsp.amount_tran,
                additional_amount = rsp.additional_amount,
                date_settle_rsp = rsp.date_settle,
                resp_code_rsp = rsp.resp_code,
                time_rsp = DateTime.Now,
                auth_tran = auth_tran,
                state = State.DONE,
                switch_key = switch_key
            };

            return await ExecuteAsync(sqltext, param);
        }
        #endregion       
    }
}
