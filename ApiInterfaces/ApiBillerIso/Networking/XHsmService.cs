using MiniAtmArtaJasa.Models;
using SWTSdk.DbEngine;
using SWTSdk.NetSocket;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiniAtmArtaJasa.Library
{
    internal class XHsmService
    {
        XHttpService _xhttp;

        public XHsmService()
        {
            _xhttp = new XHttpService();
        }

        public string TranslateKey(string baseUrl, string tpk_under_lmk)
        {
            string retval = "";

            try
            {
                CryptoKey x = GetCryptoKeys();

                TranslateKeyRequest req = new TranslateKeyRequest();
                req.tpk_under_lmk_source = tpk_under_lmk;
                req.zmk_under_lmk_dest = x.zmk_under_lmk;

                string json = JsonSerializer.Serialize(req);

                string url = baseUrl + "/hsm/translate_key_terminal";
                WSResponse wsrsp = _xhttp.Post(url, json);

                if (wsrsp.StatusCode != System.Net.HttpStatusCode.OK) return "";

                if (wsrsp == null) return "";
                if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return "";

                TranslateKeyResponse rsp =
                    JsonSerializer.Deserialize<TranslateKeyResponse>(wsrsp.MsgResponse);

                if (rsp.resp_code != "0000") return "";

                string acq_key = rsp.zpk_under_zmk;
                string acq_kcv = rsp.key_check_value;
                string iss_key = x.key_under_zmk;
                string iss_kcv = x.key_check_value;

                if (acq_key.Length == 32) acq_key = $"U{acq_key}";
                if (iss_key.Length == 32) acq_key = $"U{iss_key}";

                if (acq_key.Length == 48) acq_key = $"T{acq_key}";
                if (iss_key.Length == 48) acq_key = $"T{iss_key}";

                //result
                retval = acq_key + acq_kcv + iss_key + iss_kcv;
            }
            catch { }
           
            return retval;
        }
        private CryptoKey GetCryptoKeys()
        {
            CryptoKey retval = new CryptoKey();

            string sqltext = $@"SELECTsck.master_key as zmk_under_lmk,
                sck.key_under_lmk,sck.key_under_zmk,sck.key_check_value 
                FROM sw_crypto_keys sck join sw_nodes sn on sn.node_id =sck.node_id 
                WHERE sn.app_name='{MyApp.APPNAME}' LIMIT 1";

            DataRow rec = DbPgSql.getRow(sqltext);

            retval.zmk_under_lmk = rec["zmk_under_lmk"].ToString();
            retval.key_under_lmk = rec["key_under_lmk"].ToString();
            retval.key_under_zmk = rec["key_under_zmk"].ToString();
            retval.key_check_value = rec["key_check_value"].ToString();

            return retval;
        }
    }
}
