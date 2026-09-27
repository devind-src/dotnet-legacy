using Newtonsoft.Json;
using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.Networking;
using System.Threading.Tasks;

namespace SyncNet.HSM
{
    public class HsmService
    {
        public static async Task<HsmGenKeyResponse> GenerateKey(string SourceNode, int Timeout = 5)
        {
            //setup request
            HsmGenKeyRequest xreq = new()
            {
                node_name = SourceNode
            };

            //hit hsm web service
            string url = SdkConfig.Settings.Hsm.Url + HsmPath.GenerateKey;
            string json = JsonConvert.SerializeObject(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, Timeout);

            //validate
            if (wsrsp == null) return ReplyGenerateKeyFailed(HsmErrCode.FAILED);
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return ReplyGenerateKeyFailed(HsmErrCode.FAILED);

            //result
            return JsonConvert.DeserializeObject<HsmGenKeyResponse>(wsrsp.MsgResponse);
        }
        public static async Task<HsmGenKeyResponse> GenerateKeyTerminal(string TerminalId, int Timeout = 5)
        {
            //setup request
            HsmGenKeyRequest xreq = new()
            {
                terminal_id = TerminalId
            };

            //hit hsm web service
            string url = SdkConfig.Settings.Hsm.Url + HsmPath.GenerateKeyTerminal;
            string json = JsonConvert.SerializeObject(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, Timeout);

            //validate
            if (wsrsp == null) return ReplyGenerateKeyFailed(HsmErrCode.FAILED);
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return ReplyGenerateKeyFailed(HsmErrCode.FAILED);

            //result
            return JsonConvert.DeserializeObject<HsmGenKeyResponse>(wsrsp.MsgResponse);
        }

        public static async Task<HsmTranslatePinResponse> TranslatePinblock(string SourceNode, string DestNode, string Pinblock, string PAN, int Timeout = 5)
        {
            //set acc nr
            string accountNumber = "";
            if (!string.IsNullOrEmpty(PAN) && PAN.Length > 12)
                accountNumber = PAN.Substring(PAN.Length - 13, 12);

            //setup request
            HsmTranslatePinblock xreq = new()
            {
                node_source = SourceNode,
                node_dest = DestNode,
                source_pinblock = Pinblock,
                account_number = accountNumber
            };

            //hit hsm web service
            string url = SdkConfig.Settings.Hsm.Url + HsmPath.TranslatePinblock;
            string json = JsonConvert.SerializeObject(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, Timeout);

            //validate
            if (wsrsp == null) return ReplyTranslateFailed(HsmErrCode.FAILED);
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return ReplyTranslateFailed(HsmErrCode.FAILED);

            //result
            return JsonConvert.DeserializeObject<HsmTranslatePinResponse>(wsrsp.MsgResponse);
        }
        public static async Task<HsmTranslatePinResponse> TranslatePinblockTerminal(string TerminalId, string DestNode, string Pinblock, string PAN, int Timeout = 5)
        {
            //set acc nr
            string accountNumber = "";
            if (!string.IsNullOrEmpty(PAN) && PAN.Length > 12)
                accountNumber = PAN.Substring(PAN.Length - 13, 12);

            //setup request
            HsmTranslatePinblockTerminal xreq = new()
            {
                terminal_id = TerminalId,
                node_dest = DestNode,
                source_pinblock = Pinblock,
                account_number = accountNumber
            };

            //hit hsm web service
            string url = SdkConfig.Settings.Hsm.Url + HsmPath.TranslatePinblockTerminal;
            string json = JsonConvert.SerializeObject(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, Timeout);

            //validate
            if (wsrsp == null) return ReplyTranslateFailed(HsmErrCode.FAILED);
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return ReplyTranslateFailed(HsmErrCode.FAILED);

            //result
            return JsonConvert.DeserializeObject<HsmTranslatePinResponse>(wsrsp.MsgResponse);
        }

        public static async Task<HsmUpdateKeyResponse> UpdateKey(string NodeName, string KeyUnderZmk, int Timeout = 5)
        {
            //setup request
            HsmUpdateKeyRequest xreq = new()
            {
                node_name = NodeName,
                zpk_under_zmk = KeyUnderZmk
            };

            //hit hsm web service
            string url = SdkConfig.Settings.Hsm.Url + HsmPath.TranslateKey;
            string json = JsonConvert.SerializeObject(xreq);

            //send request
            WSResponse wsrsp = await XHttpClient.PostAsync(url, json, null, Timeout);

            //validate
            if (wsrsp == null) return ReplyUpdateKeyFailed(HsmErrCode.FAILED);
            if (string.IsNullOrEmpty(wsrsp.MsgResponse) == true) return ReplyUpdateKeyFailed(HsmErrCode.FAILED);

            //result
            return JsonConvert.DeserializeObject<HsmUpdateKeyResponse>(wsrsp.MsgResponse);
        }


        //response failed
        private static HsmGenKeyResponse ReplyGenerateKeyFailed(string RespCode)
        {
            HsmGenKeyResponse x = new()
            {
                resp_code = RespCode
            };

            return x;
        }
        private static HsmTranslatePinResponse ReplyTranslateFailed(string RespCode)
        {
            HsmTranslatePinResponse x = new()
            {
                resp_code = RespCode
            };

            return x;
        }
        private static HsmUpdateKeyResponse ReplyUpdateKeyFailed(string RespCode)
        {
            HsmUpdateKeyResponse x = new()
            {
                resp_code = RespCode
            };

            return x;
        }


        //request to HSM service
        internal class HsmGenKeyRequest
        {
            public string node_name { get; set; }
            public string terminal_id { get; set; }
        }
        internal class HsmUpdateKeyRequest
        {
            public string node_name { get; set; }
            public string zpk_under_zmk { get; set; }
        }
        internal class HsmTranslatePinblock
        {
            public string node_source { get; set; }
            public string node_dest { get; set; }
            public string source_pinblock { get; set; }
            public string account_number { get; set; }
        }
        internal class HsmTranslatePinblockTerminal
        {
            public string terminal_id { get; set; }
            public string node_dest { get; set; }
            public string source_pinblock { get; set; }
            public string account_number { get; set; }
        }
    }


    //response from HSM service
    public class HsmGenKeyResponse
    {
        public string resp_code { get; set; }
        public string resp_message { get; set; }
        public string key_under_zmk { get; set; }
        public string key_under_tmk { get; set; }
        public string key_check_value { get; set; }
    }
    public class HsmUpdateKeyResponse
    {
        public string resp_code { get; set; }
        public string resp_message { get; set; }
        public string key_check_value { get; set; }
    }
    public class HsmTranslatePinResponse
    {
        public string resp_code { get; set; }
        public string resp_message { get; set; }
        public string dest_pinblock { get; set; }
    }
}
