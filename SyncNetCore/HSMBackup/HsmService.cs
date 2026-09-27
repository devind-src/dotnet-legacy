using SWTCoreLab.DbEngine;
using SWTCoreLab.Message;
using SWTCoreLab.Models;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace SWTCoreLab.HSM
{
    public class HsmService : IDisposable
    {
        #region Variable
        //private variable
        private readonly ConcurrentDictionary<string, TerminalKey> _termKey = [];
        private readonly ConcurrentDictionary<string, HsmKey> _hsm_keys;
        private readonly HsmServiceBase _hsm;

        private string _host;
        private int _port;
        private int _timeout = 5;
        private int _header_length = 8;
        private long _header_counter;

        private bool disposedValue;

        //public variable
        public string HsmProtocol;
        #endregion

        #region Main Function
        public HsmService()
        {
            _hsm_keys = new ConcurrentDictionary<string, HsmKey>();

            //hsm service
            _hsm = new HsmServiceBase();

            //initial value
            _header_counter = 1;
        }

        public async void Start()
        {
            try
            {
                //get hsm service
                if (await InitHsmService() == -1)
                {
                    MyApp.Logger("HSM service does not exist");
                    return;
                }

                //connect to hsm service
                _hsm.Initialize(_host, _port, _header_length, _timeout);

                //load hsm keys
                await LoadHsmKeys();
                //await LoadTerminalKeys();

                //get hsm protocol
                HsmProtocol = await DbMgr.getHsmProtocol();
            }
            catch (Exception ex)
            {
                MyApp.Logger(ex.Message);
            }
        }

        public void Stop()
        {
            _hsm.Close();
        }

        public async Task Resync()
        {
            //make sure clear
            _hsm_keys.Clear();

            //reload
            await LoadHsmKeys();
            //await LoadTerminalKeys();

            //set header length
            _hsm.HeaderLength = _header_length;
            _hsm.Timeout = _timeout;
        }

        private async Task<string> SendToHsmService(Request req, string MsgToHsm)
        {
            // default response
            string result = HsmErrCode.FAILED;

            try
            {
                //write trace
                if (MyApp.IsTraceOn() == true)
                {
                    MyApp.LogTraceOutgoing("REQ", MsgToHsm, "HSM",
                        "port " + _port.ToString());
                }

                if (_hsm.IsConnected == true)
                {
                    //misc data
                    req.security.hsm_time_req = DateTime.Now;
                    req.security.hsm_data = MsgToHsm;

                    //send request to device
                    TcpResponse hsmResponse = await _hsm.SendAsync(MsgToHsm);

                    //validate response
                    if (hsmResponse == null) return result;
                    if (hsmResponse.is_success == false) return result;
                    if (string.IsNullOrEmpty(hsmResponse.resp_data) == true) return result;

                    //success
                    result = await ProcessResponse(req, hsmResponse.resp_data);
                }
                else
                {
                    //logger
                    MyApp.Logger("HSM service is not connected");
                }
            }
            catch (Exception ex)
            {
                MyApp.Logger(ex.Message);
            }

            return result;
        }

        private async Task<string> ProcessResponse(Request req, string MsgFromHsm)
        {
            // default response
            string result = HsmErrCode.FAILED;

            try
            {
                //write trace
                if (MyApp.IsTraceOn() == true)
                {
                    MyApp.LogTraceIncoming("RSP", MsgFromHsm, "HSM",
                        "port " + _port.ToString());
                }

                string command = MsgFromHsm.Substring(_header_length, 2);

                switch (command)
                {
                    case "IB":
                        result = await ResponseGenerateKey(req, MsgFromHsm);
                        break;
                    case "FB":
                        result = await ResponseTranslateKey(req, MsgFromHsm);
                        break;

                    case "CD":
                        result = ResponseTranslatePin(req, MsgFromHsm);
                        break;
                    case "BF":
                        result = ResponseVerifyPin(req, MsgFromHsm);
                        break;
                    case "JF":
                        result = ResponseTranslatePinToLmk(req, MsgFromHsm);
                        break;
                }
            }
            catch (Exception ex)
            {
                MyApp.Logger(ex.Message);
            }

            return result;
        }
        #endregion

        #region Command Request
        public async Task<string> RequestGenerateKey(Request req)
        {
            string hdr = GetNewMsgHeader();
            string mk = GetMasterKey(req.private_data.source_node);
            string prm;

            switch (mk.Substring(0, 1))
            {
                case "T":
                    prm = ";YT0";
                    break;
                case "U":
                    prm = ";XU0";
                    break;
                default:
                    prm = "";
                    break;
            }

            //request new key to hsm_service
            string msg = hdr + "IA" + mk + prm;

            //send to hsm
            return await SendToHsmService(req, msg);
        }

        public async Task<string> RequestTranslateKeyToLMK(Request req)
        {
            //** translate key from zmk to lmk **//

            string hdr = GetNewMsgHeader();
            string mk = GetMasterKey(req.private_data.source_node);

            string msg = hdr + "FA" + mk + req.security.miscdata;

            //send to hsm
            return await SendToHsmService(req, msg);
        }

        public async Task<string> RequestTranslatePinblock(Request req)
        {
            string MessageHeader = GetNewMsgHeader();
            string CommandCode = "CC";
            string SourceZPK = GetKeyUnderLMK(req.private_data.source_node);
            string DestZPK = GetKeyUnderLMK(req.private_data.sink_node);
            string MaxPinLength = "12";
            string SourcePINBlock = req.security.pindata;
            string SourcePINBlockFormat = GetPinBlockFormat(req.private_data.source_node);
            string DestPINBlockFormat = GetPinBlockFormat(req.private_data.sink_node);
            string AccountNumber = req.pan.Substring(req.pan.Length - 12 - 1, 12);

            //construct msg to hsm_service
            string MsgToHsm = MessageHeader + CommandCode + SourceZPK +
                DestZPK + MaxPinLength + SourcePINBlock +
                SourcePINBlockFormat + DestPINBlockFormat + AccountNumber;

            //send to hsm
            return await SendToHsmService(req, MsgToHsm);
        }       

        #endregion

        #region Command Response
        private async Task<string> ResponseGenerateKey(Request req, string MsgFromHsm)
        {
            string MessageHeader = MsgFromHsm.Substring(0, _header_length);
            MsgFromHsm = MsgFromHsm.Substring(_header_length, MsgFromHsm.Length - _header_length);

            string RespCode = MsgFromHsm.Substring(0, 2);
            string ErrCode = MsgFromHsm.Substring(2, 2);

            string ZPKUnderZMK = "";
            string ZPKUnderLMK = "";
            string KeyCheckValue = "";

            if (ErrCode == HsmErrCode.SUCCESS)
            {
                switch (MsgFromHsm.Substring(4, 1))
                {
                    case "Y":
                        ZPKUnderZMK = MsgFromHsm.Substring(5, 48);
                        ZPKUnderLMK = MsgFromHsm.Substring(54, 48);
                        KeyCheckValue = MsgFromHsm.Substring(102, 6);
                        break;
                    case "X":
                        ZPKUnderZMK = MsgFromHsm.Substring(5, 32);
                        ZPKUnderLMK = MsgFromHsm.Substring(38, 32);
                        KeyCheckValue = MsgFromHsm.Substring(70, 6);
                        break;
                    default:
                        ZPKUnderZMK = MsgFromHsm.Substring(4, 16);
                        ZPKUnderLMK = MsgFromHsm.Substring(20, 16);
                        KeyCheckValue = MsgFromHsm.Substring(36, 6);
                        break;
                }
            }

            if (ErrCode == HsmErrCode.SUCCESS)
            {
                string source_node = req.private_data.source_node;

                //update db
                await DbMgr.UpdateHsmKey(source_node, ZPKUnderLMK,
                    ZPKUnderZMK, KeyCheckValue);

                //update mem
                _hsm_keys[source_node].KeyUnderLMK = ZPKUnderLMK;
                _hsm_keys[source_node].KeyUnderZMK = ZPKUnderZMK;
                _hsm_keys[source_node].KeyCheckValue = KeyCheckValue;
            }

            switch (ZPKUnderZMK.Length)
            {
                case 48:
                    ZPKUnderZMK = "Y" + ZPKUnderZMK;
                    break;
                case 32:
                    ZPKUnderZMK = "X" + ZPKUnderZMK;
                    break;
            }

            //update data
            req.security.hsm_err_code = ErrCode;
            req.security.miscdata = ZPKUnderZMK + KeyCheckValue;

            return ErrCode;
        }

        private async Task<string> ResponseTranslateKey(Request req, string MsgFromHsm)
        {
            string MessageHeader = MsgFromHsm.Substring(0, _header_length);
            MsgFromHsm = MsgFromHsm.Substring(_header_length, MsgFromHsm.Length - _header_length);

            string RespCode = MsgFromHsm.Substring(0, 2);
            string ErrCode = MsgFromHsm.Substring(2, 2);

            string ZPKUnderZMK = "";
            string ZPKUnderLMK = "";
            string KeyCheckValue = "";

            if (ErrCode == HsmErrCode.SUCCESS)
            {
                switch (MsgFromHsm.Substring(4, 1))
                {
                    case "Y":
                        ZPKUnderLMK = MsgFromHsm.Substring(5, 48);
                        KeyCheckValue = MsgFromHsm.Substring(54, 6);
                        break;
                    case "X":
                        ZPKUnderLMK = MsgFromHsm.Substring(5, 32);
                        KeyCheckValue = MsgFromHsm.Substring(38, 6);
                        break;
                    default:
                        ZPKUnderLMK = MsgFromHsm.Substring(4, 16);
                        KeyCheckValue = MsgFromHsm.Substring(20, 6);
                        break;
                }
            }

            if (ErrCode == HsmErrCode.SUCCESS)
            {
                //get from request
                ZPKUnderZMK = req.security.miscdata;

                //update db
                await DbMgr.UpdateHsmKey(req.private_data.source_node,
                    ZPKUnderLMK, ZPKUnderZMK, KeyCheckValue);
            }

            //update data
            req.security.hsm_err_code = ErrCode;
            req.security.miscdata = ZPKUnderLMK + KeyCheckValue;

            return ErrCode;
        }

        private string ResponseTranslatePin(Request req, string strResp)
        {
            string MessageHeader = strResp.Substring(0, _header_length);
            strResp = strResp.Substring(_header_length, strResp.Length - _header_length);

            string PinLength;
            string DestPinBlock = "0000000000000000";
            string DestPINBlockFormat = "01";

            string RespCode = strResp.Substring(0, 2);
            string ErrCode = strResp.Substring(2, 2);

            if (ErrCode == HsmErrCode.SUCCESS)
            {
                PinLength = strResp.Substring(4, 2);
                DestPinBlock = strResp.Substring(6, 16);
                DestPINBlockFormat = strResp.Substring(22, 2);
            }

            //pin change
            if (req.security.is_pin_change == true)
                req.security.miscdata = DestPinBlock; //new pin
            else //translate pin
                req.security.pindata = DestPinBlock;

            return ErrCode;
        }

        private string ResponseTranslatePinToLmk(Request req, string strResp)
        {
            string MessageHeader = strResp.Substring(0, _header_length);
            strResp = strResp.Substring(_header_length, strResp.Length - _header_length);

            string RespCode = strResp.Substring(0, 2);
            string ErrCode = strResp.Substring(2, 2);
            string EncryptedPin = string.Empty;

            if (ErrCode == HsmErrCode.SUCCESS)
            {
                EncryptedPin = strResp.Substring(4, strResp.Length - 4);
            }

            //pin under lmk
            req.security.miscdata = EncryptedPin;

            return ErrCode;
        }

        private string ResponseVerifyPin(Request req, string strResp)
        {
            string MessageHeader = strResp.Substring(0, _header_length);
            strResp = strResp.Substring(_header_length, strResp.Length - _header_length);

            string RespCode = strResp.Substring(0, 2); //BF
            string ErrCode = strResp.Substring(2, 2);

            return ErrCode;
        }
        #endregion

        #region HSM Data
        private string GetNewMsgHeader()
        {
            Interlocked.Increment(ref _header_counter);

            //reset
            if (_header_counter >= 99999999) _header_counter = 1;

            return _header_counter.ToString().PadLeft(_header_length, '0');
        }

        private string GetMasterKey(string NodeName)
        {
            string key = "".PadLeft(16, '0');

            if (_hsm_keys.TryGetValue(NodeName, out HsmKey obj) == true)
            {
                switch (obj.MasterKey.Length)
                {
                    case 16:
                        key = obj.MasterKey;
                        break;
                    case 32:
                        key = "U" + obj.MasterKey;
                        break;
                    case 48:
                        key = "T" + obj.MasterKey;
                        break;
                }
            }

            return key;
        }

        private string GetKeyUnderLMK(string NodeName)
        {
            string key = "".PadLeft(16, '0');

            if (_hsm_keys.TryGetValue(NodeName, out HsmKey obj) == true)
            {
                switch (obj.KeyUnderLMK.Length)
                {
                    case 16:
                        key = obj.KeyUnderLMK;
                        break;
                    case 32:
                        key = "U" + obj.KeyUnderLMK;
                        break;
                    case 48:
                        key = "T" + obj.KeyUnderLMK;
                        break;
                    default:
                        break;
                }
            }

            return key;
        }

        private string GetPinBlockFormat(string NodeName)
        {
            string fmt;

            if (_hsm_keys.TryGetValue(NodeName, out HsmKey obj) == true)
                fmt = obj.PinBlockFormat;
            else
                fmt = "01"; //default format

            return fmt;
        }

        private async Task<int> InitHsmService()
        {
            //get hsm service
            DataRow rec = await DbMgr.getHsmService();
            if (rec == null) return -1;

            _timeout = Convert.ToInt32(rec["request_timeout"]);
            _header_length = Convert.ToInt32(rec["message_header"]);
            _port = Convert.ToInt32(rec["hsm_port"]);

            //hsm host
            _host = await DbMgr.getAppHost("Crypto Manager");

            return 0;
        }

        private async Task LoadHsmKeys()
        {
            DataTable tbl = await DbMgr.getHsmKeys();

            foreach (DataRow rec in tbl.Rows)
            {
                HsmKey d = new HsmKey
                {
                    NodeName = rec["node_name"].ToString(),
                    MasterKey = rec["master_key"].ToString(),
                    KeyUnderLMK = rec["key_under_lmk"].ToString(),
                    KeyUnderZMK = rec["key_under_zmk"].ToString(),
                    KeyCheckValue = rec["key_check_value"].ToString(),
                    PinBlockFormat = rec["pinblock_format"].ToString()
                };

                //add to buffer
                _hsm_keys.TryAdd(d.NodeName, d);
            }
        }

        private async Task LoadTerminalKeys()
        {
            _termKey.Clear();

            //tpk under lmk
            string sqltext = @"SELECT stk.term_id,st.serial_number,
                sm.merchant_id,sm.phone,sm.status,
                stk.master_key,stk.key_under_lmk 
                FROM sw_term_key stk
                JOIN sw_terminal st ON st.term_id=stk.term_id
                JOIN sw_merchant sm ON sm.merchant_id=st.merchant_id
                WHERE st.status='1'";

            DataTable tbl = await DbMgr.getRecords(sqltext);

            foreach (DataRow rec in tbl.Rows)
            {
                string term_id = rec["term_id"].ToString();
                string merchant_id = rec["merchant_id"].ToString();
                string serial_number = rec["serial_number"].ToString();
                string phone = rec["phone"].ToString();
                string status = rec["status"].ToString();
                string master_key = rec["master_key"].ToString();
                string key_under_lmk = rec["key_under_lmk"].ToString();

                TerminalKey T = new();
                T.merchant_id = merchant_id;
                T.serialNumber = serial_number;
                T.phoneNumber = phone;
                T.status = status;

                T.tmk_under_lmk = master_key;
                T.tpk_under_lmk = key_under_lmk;

                if (_termKey.ContainsKey(term_id) == false)
                    _termKey.TryAdd(term_id, T);
            }
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
