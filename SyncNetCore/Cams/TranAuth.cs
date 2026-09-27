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
    class TranAuth
    {
        private const string CARD_INACTIVE = "0";
        private const string CARD_ACTIVE = "1";
        private const string CARD_BLOCKED = "2";
        private const string CARD_CLOSED = "3";
        private const string CARD_PENDING = "4";
        private const string CARD_EXPIRED = "5";

        private AuthKey _issuer_key;

        public TranAuth()
        {
            _issuer_key = new AuthKey();
        }

        public async Task Resync()
        {
            await _issuer_key.Resync();
        }

        public async Task<Response> VerifyData(string node_name, string issuer, Message.Request MsgRequest)
        {
            Response ret = new Response();

            string respcode;
            string pan = MsgRequest.pan;
            string track2data = MsgRequest.security.track2data;
            string pinblock = MsgRequest.security.pindata;
            string account_type = MsgRequest.from_acc_type;

            string query = $@"SELECT offset,expire_date,service_code,private_data,seq_nr,
                hold_resp_code FROM cms_cards WHERE card_number='{pan}' AND 
                card_status='{CARD_ACTIVE}'";

            DataRow row = await DbMgr.getRow(query);

            if (row != null)
            {
                string offset = row["offset"].ToString();
                string expire_date = row["expire_date"].ToString();
                string service_code = row["service_code"].ToString();
                string private_data = row["private_data"].ToString();
                string hold_resp_code = row["hold_resp_code"].ToString();
                string seq_nr = row["seq_nr"].ToString();
                string track2db = pan + "=" + expire_date + service_code + private_data + seq_nr;

                //check track2data
                if (string.IsNullOrEmpty(track2data) == true)
                {
                    MyApp.Logger("Field 35 is mandatory", Utils.FormatMessage(MsgRequest));
                    ret.resp_code = RespCodeCms.RC56_SECUTIRY_VIOLATION;
                    return ret;
                }

                //check pinblock
                if (string.IsNullOrEmpty(pinblock) == true)
                {
                    MyApp.Logger("Field 52 is mandatory", Utils.FormatMessage(MsgRequest));
                    ret.resp_code = RespCodeCms.RC56_SECUTIRY_VIOLATION;
                    return ret;
                }

                //check whether pin has been created
                if (string.IsNullOrEmpty(offset) == true)
                {
                    MyApp.Logger("PIN Customer is not available, please create PIN for customer", Utils.FormatMessage(MsgRequest));
                    ret.resp_code = RespCodeCms.RC56_SECUTIRY_VIOLATION;
                    return ret;
                }

                //check track2data
                if (track2data != track2db)
                {
                    MyApp.Logger("Invalid track2data", Utils.FormatMessage(MsgRequest));
                    ret.resp_code = RespCodeCms.RC56_SECUTIRY_VIOLATION;
                    return ret;
                }

                //check expire card
                if (NbConvert.ToInt(expire_date) < NbConvert.ToInt(DateTime.Now.ToString("yyMM")))
                {
                    MyApp.Logger("Card expire", Utils.FormatMessage(MsgRequest));
                    ret.resp_code = RespCodeCms.RC44_CARD_EXPIRE;
                    return ret;
                }

                //get account
                string acc_number = await getAccountNumber(track2data, account_type);

                if (string.IsNullOrEmpty(acc_number) == true)
                {
                    MyApp.Logger("Invalid account type", Utils.FormatMessage(MsgRequest));
                    ret.resp_code = getAccTypeRespCode(account_type);
                    return ret;
                }

                //assign account number
                ret.additional_data = acc_number;

                //check max pin tries
                if (await getPinRetry(pan) > await getMaxPinRetry(issuer))
                {
                    DataQuery q1 = new DataQuery();
                    q1.sqltext = @"INSERT INTO cms_hotcard (card_number,issuer,resp_code,notes,last_update,update_by) 
                        VALUES(@card_number,@issuer,@resp_code,@notes,@last_update,@update_by)";

                    q1.param = new
                    {
                        card_number = pan,
                        issuer = issuer,
                        resp_code = RespCodeCms.RC75_MAX_PIN_TRIES_EXCEEDED,
                        notes = "PIN Retry Excedeed",
                        last_update = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        update_by = "System"
                    };

                    DataQuery q2 = new DataQuery();
                    q2.sqltext = @"UPDATE cms_cards SET 
                        hold_resp_code=@hold_resp_code,
                        card_status=@card_status 
                        WHERE card_number=@card_number AND 
                        card_status=@card_status_filter";

                    q2.param = new
                    {
                        hold_resp_code = RespCodeCms.RC75_MAX_PIN_TRIES_EXCEEDED,
                        card_status = CARD_BLOCKED,
                        card_number = pan,
                        card_status_filter = CARD_ACTIVE
                    };

                    List<DataQuery> queries = new List<DataQuery>();
                    queries.Add(q1);
                    queries.Add(q2);

                    await DbMgr.ExecuteAsync(queries);

                    MyApp.Logger("Max PIN tries exceeded", Utils.FormatMessage(MsgRequest));
                    ret.resp_code = RespCodeCms.RC75_MAX_PIN_TRIES_EXCEEDED;
                    return ret;
                }

                respcode = RespCodeCms.RC00_APPROVED;
            }
            else
            {
                MyApp.Logger("Active card not found", Utils.FormatMessage(MsgRequest));
                respcode = RespCodeCms.RC57_TRX_NOT_PERMITTED;
            }

            ret.resp_code = respcode;

            return ret;
        }

        public async Task<string> UpdatePIN(string pan, string newpin)
        {
            string hash = NbHash.ComputeMd5Hash(pan + newpin + AuthKey.PARRENTKEY);

            string sqltext = @"UPDATE cms_cards SET offset=@offset,hash=@hash 
                WHERE card_number=@pan AND card_status=@card_status";

            object param = new
            {
                offset = newpin,
                hash = hash,
                card_number = pan,
                card_status = CARD_ACTIVE
            };

            string respcode;
            if (await DbMgr.ExecuteAsync(sqltext, param) == 0)
                respcode = RespCodeCms.RC00_APPROVED;
            else
                respcode = RespCodeCms.RC96_SYSTEM_MALFUNCTION;

            return respcode;
        }

        public async Task<string> getAccountNumber(string track2data, string acc_type)
        {
            string query = $@"SELECT acc_number FROM cms_customer_accounts 
                INNER JOIN cms_cards ON cms_cards.id_number=cms_customer_accounts.id_number 
                WHERE card_number+'='+expire_date+service_code+private_data+CAST(seq_nr AS VARCHAR(5))='{track2data}' 
                AND acc_type='{acc_type}' AND is_primary_acc='1'";

            return await DbMgr.getFieldValue(query);
        }

        public async Task<string> getAccountNumberOld(string pan, string acc_type)
        {
            string query = $@"SELECT cms_customer_accounts.acc_number 
                FROM cms_card_account INNER JOIN cms_customer_accounts 
                ON cms_customer_accounts.issuer=cms_card_account.issuer AND 
                cms_customer_accounts.acc_number=cms_card_account.acc_number 
                WHERE cms_card_account.card_number='{pan}' AND 
                cms_customer_accounts.acc_type='{acc_type}' AND 
                cms_customer_accounts.is_primary_acc='1'";

            return await DbMgr.getFieldValue(query);
        }

        public async Task<string> getListAccountNumber(string track2data, string acc_type)
        {
            string query = $@"SELECT acc_number FROM cms_customer_accounts 
                INNER JOIN cms_cards ON cms_cards.id_number = cms_customer_accounts.id_number 
                WHERE card_number+'='+expire_date+service_code+private_data+CAST(seq_nr AS VARCHAR(5))='{track2data}' 
                AND acc_type='{acc_type}'";

            return await DbMgr.getFieldValue(query);
        }

        public async Task<string> getListAccountNumberOld(string pan, string acc_type)
        {
            string query = $@"SELECT cms_customer_accounts.acc_number 
                FROM cms_card_account INNER JOIN cms_customer_accounts 
                ON cms_customer_accounts.issuer=cms_card_account.issuer AND 
                cms_customer_accounts.acc_number=cms_card_account.acc_number 
                WHERE cms_card_account.card_number='{pan}' AND 
                cms_customer_accounts.acc_type='{acc_type}'
                GROUP BY cms_customer_accounts.acc_number";

            return await DbMgr.getFieldValue(query);
        }

        public async Task<int> UpdatePinRetry(string card_number)
        {
            string query = $@"UPDATE cms_cards 
                SET pin_retry=pin_retry+1 
                WHERE card_number=@card_number AND 
                card_status=@card_status";

            return await DbMgr.ExecuteAsync(query, new { card_number = card_number, card_status = CARD_ACTIVE });
        }

        public async Task<int> ResetPinRetry(string card_number)
        {
            string query = $@"UPDATE cms_cards SET pin_retry=0 
                WHERE card_number=@card_number AND 
                card_status=@card_status";

            return await DbMgr.ExecuteAsync(query, new { card_number = card_number, card_status = CARD_ACTIVE });
        }

        private async Task<int> getMaxPinRetry(string issuer)
        {
            string query = $"SELECT max_pin_tries FROM cms_issuers WHERE issuer='{issuer}'";
            string val = await DbMgr.getFieldValue(query);

            return NbConvert.ToInt(val);
        }

        private async Task<int> getPinRetry(string card_number)
        {
            string query = $@"SELECT pin_retry FROM cms_cards 
                WHERE card_number='{card_number}' AND card_status='{CARD_ACTIVE}'";
            string val = await DbMgr.getFieldValue(query);

            return NbConvert.ToInt(val);
        }

        private string getAccTypeRespCode(string acctype)
        {
            string respcode;//default

            if (acctype == AccountType.SAVING)
                respcode = RespCodeCms.RC53_NO_SAVING;
            else if (acctype == AccountType.CHECKING)
                respcode = RespCodeCms.RC52_NO_CHECKING;
            else if (acctype == AccountType.CREDIT_CARD)
                respcode = RespCodeCms.RC39_NO_CREDIT_ACCOUNT;
            else
                respcode = RespCodeCms.RC42_NO_UNIVERSAL_ACCOUNT;

            return respcode;
        }

        private string getExpireDate(string track2data)
        {
            string[] tmp = track2data.Split('=');

            return tmp[2].Substring(0, 4);
        }

        private async Task<List<string>> getBins(string nodename)
        {
            List<string> bins = new List<string>();

            string query = $@"SELECT bin_nr FROM sw_nodes 
                JOIN sw_routes_by_bin ON sw_routes_by_bin.node_id=sw_nodes.node_id 
                JOIN sw_bins ON sw_bins.group_id=sw_routes_by_bin.group_id 
                WHERE node_name='{nodename}'";

            DataTable tbl = await DbMgr.getRecords(query);
            foreach (DataRow rec in tbl.Rows)
            {
                bins.Add(rec["bin_nr"].ToString());
            }

            return bins;
        }
    }
}
