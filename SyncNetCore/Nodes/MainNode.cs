using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Library;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SyncNet.Nodes
{
    class MainNode
    {
        //variable
        private readonly Dictionary<string, SourceNode> _src = [];
        private readonly Dictionary<string, SinkNode> _snk = [];

        public async Task Start()
        {
            try
            {
                DataTable tbl = await DbMgr.GetNodes();

                foreach (DataRow row in tbl.Rows)
                {
                    var src = new SourceNode
                    {
                        NodeName = row["node_name"].ToString(),
                        InstID = row["inst_id"].ToString(),
                        Port = System.Convert.ToInt32(row["port_in"]),
                        AutoReversal = row["auto_reversal"].ToString(),
                        AutoReplyReversal = row["auto_reply_reversal"].ToString(),
                        EnableClosing = Convert.ToInt16(row["enable_closing"].ToString()),
                        ClosingTimeStart = row["time_start"].ToString(),
                        ClosingTimeEnd = row["time_end"].ToString(),
                        PinTranslate = row["pin_translate"].ToString(),
                        ProtectSensitiveData = row["sensitive_data"].ToString(),
                        CalendarName = row["business_calendar"].ToString()
                    };

                    await src.Start();

                    //add to buffer
                    _src.Add(src.NodeName, src);

                    var snk = new SinkNode
                    {
                        NodeName = row["node_name"].ToString(),
                        InstID = row["inst_id"].ToString(),
                        Port = System.Convert.ToInt32(row["port_out"]),
                        AutoReversal = row["auto_reversal"].ToString(),
                        ProviderService = row["provider_service"].ToString(),
                        AuthService = row["auth_service"].ToString(),
                        Issuer = row["issuer"].ToString(),
                        RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                        AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                        MaxRetrySend = Convert.ToInt16(row["saf_limit"]),
                        ProtectSensitiveData = row["sensitive_data"].ToString(),
                        CalendarName = row["business_calendar"].ToString(),
                        SendCutover = row["send_cutover_msg"].ToString(),
                        SaveRepeatReversal = row["save_repeat_reversal"].ToString()
                    };

                    await snk.Start();

                    //add to buffer
                    _snk.Add(snk.NodeName, snk);
                }
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }
        }

        public async Task Stop()
        {
            foreach (string key in _src.Keys)
            {
                await _src[key].Stop();
                await _snk[key].Stop();
            }

            _src.Clear();
            _snk.Clear();
        }

        public async Task Resync()
        {
            await CheckNewNode();
            await CheckNodeDelete();
            await CheckNodeChange();
        }

        public async Task SendToSource(string NodeName, Message.Response MsgResponse)
        {
            await _src[NodeName].Send(MsgResponse);
        }

        public async Task SendToSink(string NodeName, Message.Request MsgRequest)
        {
            await _snk[NodeName].Send(MsgRequest);
        }

        public async Task SendCutover(string CalendarName)
        {
            foreach (SinkNode s in _snk.Values)
            {
                if (s.CalendarName == CalendarName && s.SendCutover == "1")
                {
                    var MsgRequest = new Message.Request
                    {
                        tran_type = TranType.CUTOVER,
                        trace_number = NbSystem.GetRandomNumber(12),
                        datetime_tran = DateTime.Now.ToString("yyyyMMddHHmmss")
                    };

                    //send
                    await s.Send(MsgRequest);
                }
            }
        }

        public TypeServices GetServices(string NodeName)
        {
            var retval = new TypeServices
            {
                AuthService = _snk[NodeName].AuthService,
                ProviderService = _snk[NodeName].ProviderService
            };

            return retval;
        }

        public string GetIssuer(string NodeName)
        {
            return _snk[NodeName].Issuer;
        }

        private async Task CheckNewNode()
        {
            try
            {
                DataTable tbl = await DbMgr.GetNodes();

                foreach (DataRow row in tbl.Rows)
                {
                    string node_name = row["node_name"].ToString();
                    bool bFound = _src.ContainsKey(node_name);

                    if (bFound == false)
                    {
                        var src = new SourceNode
                        {
                            NodeName = row["node_name"].ToString(),
                            InstID = row["inst_id"].ToString(),
                            Port = System.Convert.ToInt32(row["port_in"]),
                            AutoReversal = row["auto_reversal"].ToString(),
                            AutoReplyReversal = row["auto_reply_reversal"].ToString(),
                            EnableClosing = Convert.ToInt16(row["enable_closing"].ToString()),
                            ClosingTimeStart = row["time_start"].ToString(),
                            ClosingTimeEnd = row["time_end"].ToString(),
                            PinTranslate = row["pin_translate"].ToString(),
                            ProtectSensitiveData = row["sensitive_data"].ToString(),
                            CalendarName = row["business_calendar"].ToString()
                        };

                        await src.Start();

                        //add to buffer
                        _src.Add(src.NodeName, src);

                        var snk = new SinkNode
                        {
                            NodeName = row["node_name"].ToString(),
                            InstID = row["inst_id"].ToString(),
                            Port = System.Convert.ToInt32(row["port_out"]),
                            AutoReversal = row["auto_reversal"].ToString(),
                            ProviderService = row["provider_service"].ToString(),
                            AuthService = row["auth_service"].ToString(),
                            Issuer = row["issuer"].ToString(),
                            RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                            AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                            MaxRetrySend = Convert.ToInt16(row["saf_limit"]),
                            ProtectSensitiveData = row["sensitive_data"].ToString(),
                            CalendarName = row["business_calendar"].ToString(),
                            SendCutover = row["send_cutover_msg"].ToString(),
                            SaveRepeatReversal = row["save_repeat_reversal"].ToString()
                        };

                        await snk.Start();

                        //add to buffer
                        _snk.Add(snk.NodeName, snk);
                    }
                }

            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }
        }

        private async Task CheckNodeDelete()
        {
            try
            {
                DataTable tbl = await DbMgr.GetNodes();
                bool bFound;

                foreach (string key in _src.Keys)
                {
                    bFound = false;

                    foreach (DataRow row in tbl.Rows)
                    {
                        string node_name = row["node_name"].ToString();

                        if (node_name == _src[key].NodeName)
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (bFound == false)
                    {
                        //stop node
                        await _src[key].Stop();
                        await _snk[key].Stop();

                        //remove
                        _src.Remove(key);
                        _snk.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }
        }

        private async Task CheckNodeChange()
        {
            try
            {
                DataTable tbl = await DbMgr.GetNodes();
                bool bRestart = false;

                foreach (DataRow row in tbl.Rows)
                {
                    string key = row["node_name"].ToString();
                    string inst_id = row["inst_id"].ToString();
                    string auto_rev = row["auto_reversal"].ToString();
                    string auto_reply_reversal = row["auto_reply_reversal"].ToString();
                    string provider_service = row["provider_service"].ToString();
                    string auth_service = row["auth_service"].ToString();
                    string issuer = row["issuer"].ToString();
                    string sensitive_data = row["sensitive_data"].ToString();
                    string pin_translate = row["pin_translate"].ToString();
                    string time_start = row["time_start"].ToString();
                    string time_end = row["time_end"].ToString();
                    string calendar_name = row["business_calendar"].ToString();
                    string send_cutover = row["send_cutover_msg"].ToString();
                    string save_repeat_reversal = row["save_repeat_reversal"].ToString();

                    int port_in = System.Convert.ToInt32(row["port_in"]);
                    int port_out = System.Convert.ToInt32(row["port_out"]);
                    int enable_closing = Convert.ToInt16(row["enable_closing"]);
                    int saf_limit = Convert.ToInt16(row["saf_limit"]);
                    int req_timeout = Convert.ToInt16(row["request_timeout"]);
                    int adv_timeout = Convert.ToInt16(row["advice_timeout"]);

                    if (_src.ContainsKey(key) == true)
                    {
                        if (_src[key].Port != port_in)
                            bRestart = true;
                        else
                            bRestart = false;

                        _src[key].InstID = inst_id;
                        _src[key].Port = port_in;
                        _src[key].AutoReversal = auto_rev;
                        _src[key].AutoReplyReversal = auto_reply_reversal;
                        _src[key].EnableClosing = enable_closing;
                        _src[key].ClosingTimeStart = time_start;
                        _src[key].ClosingTimeEnd = time_end;
                        _src[key].PinTranslate = pin_translate;
                        _src[key].ProtectSensitiveData = sensitive_data;
                        _src[key].CalendarName = calendar_name;

                        if (bRestart == true)
                        {
                            await _src[key].Stop();
                            await _src[key].Start();
                        }

                        if (_snk[key].Port != port_out)
                            bRestart = true;
                        else
                            bRestart = false;

                        _snk[key].InstID = inst_id;
                        _snk[key].Port = port_out;
                        _snk[key].AutoReversal = auto_rev;
                        _snk[key].ProviderService = provider_service;
                        _snk[key].AuthService = auth_service;
                        _snk[key].Issuer = issuer;
                        _snk[key].RequestTimeout = req_timeout;
                        _snk[key].AdviceTimeout = adv_timeout;
                        _snk[key].MaxRetrySend = saf_limit;
                        _snk[key].ProtectSensitiveData = sensitive_data;
                        _snk[key].CalendarName = calendar_name;
                        _snk[key].SendCutover = send_cutover;
                        _snk[key].SaveRepeatReversal = save_repeat_reversal;

                        if (bRestart == true)
                        {
                            await _snk[key].Stop();
                            await _snk[key].Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }
        }
    }
}
