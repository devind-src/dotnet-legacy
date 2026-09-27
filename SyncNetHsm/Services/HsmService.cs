using Newtonsoft.Json;
using SyncNet.Common;
using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Helpers;
using SyncNet.Logging;
using SyncNet.Models.HsmDeviceModel;
using SyncNet.Models.HsmServiceModel;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    public class HsmService
    {
        private readonly ConcurrentDictionary<string, HsmTerminalKey> _listTermKey = [];
        private readonly ConcurrentDictionary<string, HsmNodeKey> _listNodeKey = [];
        private readonly ConcurrentDictionary<string, int> _listNodes = [];

        private readonly HsmEmulator _hsmEmulator;
        private readonly HsmThales _hsmThales;

        private readonly CustomLogger _logger;
        private readonly ApiClient _apiClient;
        private readonly DbMgr _dbMgr;

        private bool _initialized = false;

        public HsmService(CustomLogger logger, ApiClient apiClient, DbMgr dbMgr, HsmEmulator hsmEmulator, HsmThales hsmThales)
        {
            _apiClient = apiClient;
            _hsmEmulator = hsmEmulator;
            _hsmThales = hsmThales;
            _logger = logger;
            _dbMgr = dbMgr;
        }

        public async Task Initialize()
        {
            try
            {
                //first validation
                if (_initialized) return;

                //init hsm emulator
                _hsmEmulator.Initialize();

                //connect to hsm device using protocol tcp/ip
                if (AppConfig.HsmConnectionMode == ConnType.Persistent)
                    await _apiClient.Connect();//first connection

                await LoadNodes();
                await LoadNodeKeys();
                await LoadTerminalKeys();

                _initialized = true;
            }
            catch (Exception ex)
            {
                _logger.Log("HSM Service Initialize: " + ex.Message);
            }
        }

        public async Task<string> GenerateKey(string Body)
        {
            string ret;

            try
            {
                XGenerateKeyRequest req =
                  JsonConvert.DeserializeObject<XGenerateKeyRequest>(Body);

                //check node key
                if (_listNodeKey.ContainsKey(req.node_name) == false)
                {
                    if (await GetNodeKey(req.node_name) == false)
                        return DataHelper.GetBaseResponseString(ErrMessage.InterfaceNotFound);
                }

                //get zmk
                string zmk_under_lmk = _listNodeKey[req.node_name].zmk_under_lmk;

                //request key to hsm
                GenerateZPKResponse rspHsm;
                if (AppConfig.HsmEmulator == true)
                    rspHsm = _hsmEmulator.GenerateZPK(zmk_under_lmk);
                else
                    rspHsm = await _hsmThales.GenerateZPK(zmk_under_lmk);

                //validate
                if (rspHsm == null) return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);
                if (rspHsm.resp_code != "00") return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);

                //update memory
                _listNodeKey[req.node_name].key_under_lmk = rspHsm.key_under_lmk;
                _listNodeKey[req.node_name].key_under_zmk = rspHsm.key_under_zmk;
                _listNodeKey[req.node_name].key_check_value = rspHsm.key_check_value;

                //update db
                await UpdateNodeKey(req.node_name, rspHsm.key_under_lmk,
                    rspHsm.key_under_zmk, rspHsm.key_check_value);

                //result
                XGenerateKeyResponse rsp = new()
                {
                    resp_code = rspHsm.resp_code,
                    resp_message = rspHsm.resp_message,
                    key_under_zmk = rspHsm.key_under_zmk,
                    key_check_value = rspHsm.key_check_value
                };

                ret = JsonConvert.SerializeObject(rsp);
            }
            catch (Exception ex)
            {
                ret = DataHelper.GetBaseResponseString(ErrMessage.GeneralError);

                _logger.Log(ex.Message);
            }

            return ret;
        }

        public async Task<string> GenerateKeyTerminal(string Body)
        {
            string ret;

            try
            {
                XGenerateKeyTerminalRequest req =
                  JsonConvert.DeserializeObject<XGenerateKeyTerminalRequest>(Body);

                //check terminal key
                if (_listTermKey.ContainsKey(req.terminal_id) == false)
                {
                    if (await GetTerminalKey(req.terminal_id) == false)
                        return DataHelper.GetBaseResponseString(ErrMessage.TerminalNotFound);
                }

                //get tmk
                string tmk_under_lmk = _listTermKey[req.terminal_id].tmk_under_lmk;

                //request key to hsm
                GenerateTPKResponse rspHsm;
                if (AppConfig.HsmEmulator == true)
                    rspHsm = _hsmEmulator.GenerateTPK(tmk_under_lmk);
                else
                    rspHsm = await _hsmThales.GenerateTPK(tmk_under_lmk);

                //validate
                if (rspHsm == null) return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);
                if (rspHsm.resp_code != "00") return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);

                //update memory
                _listTermKey[req.terminal_id].key_under_lmk = rspHsm.key_under_lmk;
                _listTermKey[req.terminal_id].key_under_tmk = rspHsm.key_under_tmk;
                _listTermKey[req.terminal_id].key_check_value = rspHsm.key_check_value;

                //update db
                await UpdateTerminalKey(req.terminal_id, rspHsm.key_under_lmk,
                    rspHsm.key_under_tmk, rspHsm.key_check_value);

                //result
                XGenerateKeyTerminalResponse rsp = new()
                {
                    resp_code = rspHsm.resp_code,
                    resp_message = rspHsm.resp_message,
                    key_under_tmk = rspHsm.key_under_tmk,
                    key_check_value = rspHsm.key_check_value
                };

                ret = JsonConvert.SerializeObject(rsp);
            }
            catch (Exception ex)
            {
                ret = DataHelper.GetBaseResponseString(ErrMessage.GeneralError);

                _logger.Log(ex.Message);
            }

            return ret;
        }

        public async Task<string> TranslateKeyToLmk(string Body)
        {
            string ret;

            try
            {
                XTranslateKeyRequest req =
                  JsonConvert.DeserializeObject<XTranslateKeyRequest>(Body);

                //check node key
                if (_listNodeKey.ContainsKey(req.node_name) == false)
                {
                    if (await GetNodeKey(req.node_name) == false)
                        return DataHelper.GetBaseResponseString(ErrMessage.InterfaceNotFound);
                }

                //get zmk
                string zmk_under_lmk = _listNodeKey[req.node_name].zmk_under_lmk;
                string key_check_value = _listNodeKey[req.node_name].key_check_value;

                //setup request
                TranslateKeyRequest reqHsm = new();
                reqHsm.zmk_under_lmk = zmk_under_lmk;
                reqHsm.key_under_zmk = req.zpk_under_zmk;

                //request key to hsm
                TranslateKeyResponse rspHsm;
                if (AppConfig.HsmEmulator == true)
                    rspHsm = _hsmEmulator.TranslateKeyToLmk(reqHsm);
                else
                    rspHsm = await _hsmThales.TranslateKeyToLmk(reqHsm);

                //validate
                if (rspHsm == null) return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);
                if (rspHsm.resp_code != "00") return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);

                //validate key check value
                if (rspHsm.key_check_value.Substring(0, 6) != key_check_value.Substring(0, 6))
                    return DataHelper.GetBaseResponseString(ErrMessage.KeyCheckValueError);

                //update memory
                _listNodeKey[req.node_name].key_under_lmk = rspHsm.key_under_lmk;

                //update db
                await UpdateNodeKey(req.node_name, rspHsm.key_under_lmk);

                //result
                XTranslateKeyResponse rsp = new()
                {
                    resp_code = rspHsm.resp_code,
                    resp_message = rspHsm.resp_message,
                    key_check_value = rspHsm.key_check_value
                };

                ret = JsonConvert.SerializeObject(rsp);
            }
            catch (Exception ex)
            {
                ret = DataHelper.GetBaseResponseString(ErrMessage.GeneralError);

                _logger.Log(ex.Message);
            }

            return ret;
        }

        public async Task<string> TranslatePinblock(string Body)
        {
            string ret;

            try
            {
                XTranslatePinblockRequest req =
                  JsonConvert.DeserializeObject<XTranslatePinblockRequest>(Body);

                //get key source & dest
                string key_under_lmk_source = _listNodeKey[req.node_source].key_under_lmk;
                string key_under_lmk_dest = _listNodeKey[req.node_dest].key_under_lmk;

                //validate
                if (string.IsNullOrEmpty(key_under_lmk_source))
                    return DataHelper.GetBaseResponseString(ErrMessage.InterfaceNotFoundCustom.Replace("%NODE%", req.node_source));

                if (string.IsNullOrEmpty(key_under_lmk_dest))
                    return DataHelper.GetBaseResponseString(ErrMessage.InterfaceNotFoundCustom.Replace("%NODE%", req.node_dest));

                //setup request
                TranslatePinblockRequest reqHsm = new()
                {
                    key_under_lmk_source = key_under_lmk_source,
                    key_under_lmk_dest = key_under_lmk_dest,
                    source_pinblock = req.source_pinblock,
                    account_number = req.account_number
                };

                //request translate pinblock to hsm
                TranslatePinblockResponse rspHsm;
                if (AppConfig.HsmEmulator == true)
                    rspHsm = _hsmEmulator.TranslatePinblock(reqHsm);
                else
                    rspHsm = await _hsmThales.TranslatePinblock(reqHsm);

                //validate
                if (rspHsm == null) return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);
                if (rspHsm.resp_code != "00") return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);

                //result
                XTranslatePinblockResponse rsp = new()
                {
                    resp_code = rspHsm.resp_code,
                    resp_message = rspHsm.resp_message,
                    dest_pinblock = rspHsm.dest_pinblock
                };

                ret = JsonConvert.SerializeObject(rsp);
            }
            catch (Exception ex)
            {
                ret = DataHelper.GetBaseResponseString(ErrMessage.GeneralError);

                _logger.Log(ex.Message);
            }

            return ret;
        }

        public async Task<string> TranslatePinblockTerminal(string Body)
        {
            string ret;

            try
            {
                XTranslatePinblockTerminalRequest req =
                  JsonConvert.DeserializeObject<XTranslatePinblockTerminalRequest>(Body);

                //get key source & dest
                string key_under_lmk_source = _listTermKey[req.terminal_id].key_under_lmk;
                string key_under_lmk_dest = _listNodeKey[req.node_dest].key_under_lmk;

                //validate
                if (string.IsNullOrEmpty(key_under_lmk_source))
                    return DataHelper.GetBaseResponseString(ErrMessage.TerminalNotFoundCustom.Replace("%TID%", req.terminal_id));

                if (string.IsNullOrEmpty(key_under_lmk_dest))
                    return DataHelper.GetBaseResponseString(ErrMessage.InterfaceNotFoundCustom.Replace("%NODE%", req.node_dest));

                //setup request
                TranslatePinblockRequest reqHsm = new()
                {
                    key_under_lmk_source = key_under_lmk_source,
                    key_under_lmk_dest = key_under_lmk_dest,
                    source_pinblock = req.source_pinblock,
                    account_number = req.account_number
                };

                //request translate pinblock to hsm
                TranslatePinblockResponse rspHsm;
                if (AppConfig.HsmEmulator == true)
                    rspHsm = _hsmEmulator.TranslatePinblockTerminal(reqHsm);
                else
                    rspHsm = await _hsmThales.TranslatePinblockTerminal(reqHsm);

                //validate
                if (rspHsm == null) return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);
                if (rspHsm.resp_code != "00") return DataHelper.GetBaseResponseString(ErrMessage.RequestHsmError);

                //result
                XTranslatePinblockResponse rsp = new()
                {
                    resp_code = rspHsm.resp_code,
                    resp_message = rspHsm.resp_message,
                    dest_pinblock = rspHsm.dest_pinblock
                };

                ret = JsonConvert.SerializeObject(rsp);
            }
            catch (Exception ex)
            {
                ret = DataHelper.GetBaseResponseString(ErrMessage.GeneralError);

                _logger.Log(ex.Message);
            }

            return ret;
        }

        #region Database
        private async Task LoadNodes()
        {
            _listNodes.Clear();

            string sqltext = @"SELECT node_id,node_name FROM sw_nodes";

            DataTable tbl = await _dbMgr.GetRecords(sqltext);

            foreach (DataRow rec in tbl.Rows)
            {
                int node_id = rec["node_id"].ToString().ToNumber();
                string node_name = rec["node_name"].ToString();

                //add to buffer
                _listNodes.TryAdd(node_name, node_id);
            }
        }

        private async Task LoadNodeKeys()
        {
            _listNodeKey.Clear();

            string sqltext = @"SELECT T2.node_name,T1.master_key,T1.key_under_lmk,T1.key_under_zmk,T1.key_check_value,T1.pinblock_format 
                FROM sw_crypto_keys T1 JOIN sw_nodes T2 ON T2.node_id = T1.node_id";

            DataTable tbl = await _dbMgr.GetRecords(sqltext);

            foreach (DataRow rec in tbl.Rows)
            {
                HsmNodeKey d = new HsmNodeKey
                {
                    node_name = rec["node_name"].ToString(),
                    zmk_under_lmk = rec["master_key"].ToString(),
                    key_under_lmk = rec["key_under_lmk"].ToString(),
                    key_under_zmk = rec["key_under_zmk"].ToString(),
                    key_check_value = rec["key_check_value"].ToString(),
                    pinblock_format = rec["pinblock_format"].ToString()
                };

                //add to buffer
                _listNodeKey.TryAdd(d.node_name, d);
            }
        }

        private async Task LoadTerminalKeys()
        {
            _listTermKey.Clear();

            //tpk under lmk
            string sqltext = @"SELECT stk.term_id,st.serial_number,
                sm.merchant_id,sm.status,
                stk.master_key,stk.key_under_lmk,stk.key_under_zmk 
                FROM sw_term_key stk
                JOIN sw_terminal st ON st.term_id=stk.term_id
                JOIN sw_merchant sm ON sm.merchant_id=st.merchant_id
                WHERE st.status='1'";

            DataTable tbl = await _dbMgr.GetRecords(sqltext);

            foreach (DataRow rec in tbl.Rows)
            {
                string term_id = rec["term_id"].ToString();
                string merchant_id = rec["merchant_id"].ToString();
                string serial_number = rec["serial_number"].ToString();
                string status = rec["status"].ToString();
                string tmk_under_lmk = rec["master_key"].ToString();
                string key_under_lmk = rec["key_under_lmk"].ToString();
                string key_under_tmk = rec["key_under_zmk"].ToString();

                HsmTerminalKey T = new()
                {
                    merchant_id = merchant_id,
                    serial_number = serial_number,
                    status = status,

                    tmk_under_lmk = tmk_under_lmk,
                    key_under_lmk = key_under_lmk,
                    key_under_tmk = key_under_tmk
                };

                //add to buffer
                _listTermKey.TryAdd(term_id, T);
            }
        }

        private async Task<bool> GetNodeKey(string NodeName)
        {
            string sqltext = @"SELECT T2.node_name,T1.master_key,T1.key_under_lmk,T1.key_under_zmk,
                T1.key_check_value,T1.pinblock_format 
                FROM sw_crypto_keys T1 JOIN sw_nodes T2 ON T2.node_id = T1.node_id
                WHERE T2.node_name=@node_name";

            Dictionary<string, object> parameters = new()
            {
                { "node_name", NodeName }
            };

            DataRow rec = await _dbMgr.GetRow(sqltext, parameters);
            if (rec == null) return false;

            HsmNodeKey d = new HsmNodeKey
            {
                node_name = rec["node_name"].ToString(),
                zmk_under_lmk = rec["master_key"].ToString(),
                key_under_lmk = rec["key_under_lmk"].ToString(),
                key_under_zmk = rec["key_under_zmk"].ToString(),
                key_check_value = rec["key_check_value"].ToString(),
                pinblock_format = rec["pinblock_format"].ToString()
            };

            //add to buffer
            if (_listNodeKey.ContainsKey(d.node_name) == false)
                _listNodeKey.TryAdd(d.node_name, d);

            return true;
        }

        private async Task<bool> GetTerminalKey(string terminal_id)
        {
            //tpk under lmk
            string sqltext = $@"SELECT stk.term_id,st.serial_number,
                sm.merchant_id,sm.status,
                stk.master_key,stk.key_under_lmk,stk.key_under_zmk 
                FROM sw_term_key stk
                JOIN sw_terminal st ON st.term_id=stk.term_id
                JOIN sw_merchant sm ON sm.merchant_id=st.merchant_id
                WHERE st.status='1' AND st.term_id=@terminal_id";

            Dictionary<string, object> parameters = new()
            {
                { "terminal_id", terminal_id }
            };

            DataRow rec = await _dbMgr.GetRow(sqltext, parameters);
            if (rec == null) return false;

            string term_id = rec["term_id"].ToString();
            string merchant_id = rec["merchant_id"].ToString();
            string serial_number = rec["serial_number"].ToString();
            string status = rec["status"].ToString();
            string tmk_under_lmk = rec["master_key"].ToString();
            string key_under_lmk = rec["key_under_lmk"].ToString();
            string key_under_tmk = rec["key_under_zmk"].ToString();

            HsmTerminalKey T = new()
            {
                merchant_id = merchant_id,
                serial_number = serial_number,
                status = status,

                tmk_under_lmk = tmk_under_lmk,
                key_under_lmk = key_under_lmk,
                key_under_tmk = key_under_tmk
            };

            if (_listTermKey.ContainsKey(term_id) == false)
                _listTermKey.TryAdd(term_id, T);

            return true;
        }

        private async Task UpdateNodeKey(string NodeName, string KeyUnderLMK, string KeyUnderZMK, string KeyCheckValue)
        {
            int node_id = GetNodeId(NodeName);

            string sqltext = $@"UPDATE sw_crypto_keys SET 
                key_under_lmk=@key_under_lmk,
                key_under_zmk=@key_under_zmk,
                key_check_value=@key_check_value 
                WHERE node_id=@node_id";

            object param = new
            {
                key_under_lmk = KeyUnderLMK,
                key_under_zmk = KeyUnderZMK,
                key_check_value = KeyCheckValue,
                node_id = node_id
            };

            await _dbMgr.ExecuteAsync(sqltext, param);
        }

        private async Task UpdateNodeKey(string NodeName, string KeyUnderLMK)
        {
            int node_id = GetNodeId(NodeName);

            string sqltext = $@"UPDATE sw_crypto_keys SET 
                key_under_lmk=@key_under_lmk 
                WHERE node_id=@node_id";

            object param = new
            {
                key_under_lmk = KeyUnderLMK,
                node_id = node_id
            };

            await _dbMgr.ExecuteAsync(sqltext, param);
        }

        private async Task UpdateTerminalKey(string TerminalId, string KeyUnderLMK, string KeyUnderZMK, string KeyCheckValue)
        {
            string sqltext = $@"UPDATE sw_term_key SET 
                key_under_lmk=@key_under_lmk,
                key_under_zmk=@key_under_zmk,
                key_check_value=@key_check_value 
                WHERE term_id=@term_id";

            object param = new
            {
                key_under_lmk = KeyUnderLMK,
                key_under_zmk = KeyUnderZMK,
                key_check_value = KeyCheckValue,
                term_id = TerminalId
            };

            await _dbMgr.ExecuteAsync(sqltext, param);
        }

        private int GetNodeId(string NodeName)
        {
            int ret = -1;

            if (_listNodes.TryGetValue(NodeName, out int node_id) == true)
                ret = node_id;

            return ret;
        }
        #endregion
    }
}
