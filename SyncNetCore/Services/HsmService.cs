using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.Message;
using SyncNet.Models.HsmService;
using SyncNet.Models.Networking;
using SyncNet.Networking;
using System.Text.Json;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    internal class HsmService
    {
        public async Task<string> GenerateKey(Request req)
        {
            //setup request
            XGenerateKeyRequest xreq = new()
            {
                node_name = req.private_data.source_node
            };

            //hit hsm web service
            string url = AppConfig.HsmUrl + HsmPath.GenerateKey;
            string json = JsonSerializer.Serialize(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json,null, AppConfig.HsmTimeout);

            //validate
            if (wsrsp == null) return HsmErrCode.FAILED;
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return HsmErrCode.FAILED;

            //extract response
            XGenerateKeyResponse result = JsonSerializer.Deserialize<XGenerateKeyResponse>(wsrsp.MsgResponse);

            //update data
            req.security.hsm_err_code = result.resp_code;
            req.security.miscdata = result.key_under_zmk + result.key_check_value;

            return result.resp_code;
        }

        public async Task<string> GenerateKeyTerminal(Request req)
        {
            //setup request
            XGenerateKeyTerminalRequest xreq = new()
            {
                terminal_id = req.terminal_id
            };

            //hit hsm web service
            string url = AppConfig.HsmUrl + HsmPath.GenerateKeyTerminal;
            string json = JsonSerializer.Serialize(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, AppConfig.HsmTimeout);

            //validate
            if (wsrsp == null) return HsmErrCode.FAILED;
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return HsmErrCode.FAILED;

            //extract response
            XGenerateKeyTerminalResponse result = JsonSerializer.Deserialize<XGenerateKeyTerminalResponse>(wsrsp.MsgResponse);

            //update data
            req.security.hsm_err_code = result.resp_code;
            req.security.miscdata = result.key_under_tmk + result.key_check_value;

            return result.resp_code;
        }

        public async Task<string> TranslateKey(Request req)
        {
            //setup request
            XTranslateKeyRequest xreq = new()
            {
                node_name = req.private_data.source_node,
                zpk_under_zmk = req.security.miscdata
            };

            //hit hsm web service
            string url = AppConfig.HsmUrl + HsmPath.TranslateKey;
            string json = JsonSerializer.Serialize(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, AppConfig.HsmTimeout);

            //validate
            if (wsrsp == null) return HsmErrCode.FAILED;
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return HsmErrCode.FAILED;

            //extract response
            XTranslateKeyResponse result = JsonSerializer.Deserialize<XTranslateKeyResponse>(wsrsp.MsgResponse);

            //update data
            req.security.hsm_err_code = result.resp_code;

            return result.resp_code;
        }

        public async Task<string> TranslatePinblock(Request req)
        {
            //setup request
            XTranslatePinblockRequest xreq = new()
            {
                node_source = req.private_data.source_node,
                node_dest = req.private_data.sink_node,
                source_pinblock = req.security.pindata,
                account_number = req.pan.Substring(req.pan.Length - 13, 12)
            };

            //hit hsm web service
            string url = AppConfig.HsmUrl + HsmPath.TranslatePinblock;
            string json = JsonSerializer.Serialize(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, null, AppConfig.HsmTimeout);

            //validate
            if (wsrsp == null) return HsmErrCode.FAILED;
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return HsmErrCode.FAILED;

            //extract response
            XTranslatePinblockResponse result = JsonSerializer.Deserialize<XTranslatePinblockResponse>(wsrsp.MsgResponse);

            //update data
            req.security.hsm_err_code = result.resp_code;
            req.security.pindata = result.dest_pinblock;

            return result.resp_code;
        }

        public async Task<string> TranslatePinblockTerminal(Request req)
        {
            //setup request
            XTranslatePinblockTerminalRequest xreq = new()
            {
                terminal_id = req.terminal_id,
                node_dest = req.private_data.sink_node,
                source_pinblock = req.security.pindata,
                account_number = req.pan.Substring(req.pan.Length - 13, 12)
            };

            //hit hsm web service
            string url = AppConfig.HsmUrl + HsmPath.TranslatePinblockTerminal;
            string json = JsonSerializer.Serialize(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, AppConfig.HsmTimeout);

            //validate
            if (wsrsp == null) return HsmErrCode.FAILED;
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return HsmErrCode.FAILED;

            //extract response
            XTranslatePinblockResponse result = JsonSerializer.Deserialize<XTranslatePinblockResponse>(wsrsp.MsgResponse);

            //update data
            req.security.hsm_err_code = result.resp_code;
            req.security.pindata = result.dest_pinblock;

            return result.resp_code;
        }
    }
}
