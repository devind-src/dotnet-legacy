using SyncNet.Constants;
using SyncNet.DbRepository;
using SyncNet.Library;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SyncNet.Nodes
{
    class NodeInternal
    {
        //variable
        private readonly Dictionary<string, SourceNode> _src = [];
        private readonly Dictionary<string, SinkNode> _snk = [];

        private readonly NbLogger _logger;
        private readonly DbMgr _dbMgr;

        public NodeInternal()
        {
            _logger = new NbLogger(AppProcessor.APPNAME);
            _dbMgr = new DbMgr();
        }

        public async Task Start(string AppName)
        {
            try
            {
                DataTable tbl = await _dbMgr.GetNodes(AppName);

                foreach (DataRow row in tbl.Rows)
                {
                    if (row["category"].ToString() == NodeCategory.MERCHANT)
                    {
                        var src = new SourceNode
                        {
                            NodeName = row["node_name"].ToString(),
                            InstID = row["inst_id"].ToString(),
                            Port = System.Convert.ToInt32(row["port_in"]),
                            AutoReversal = row["auto_reversal"].ToString(),
                            RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                            AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                            MaxRetrySend = Convert.ToInt16(row["saf_limit"])
                        };

                        await src.Start();

                        //add to buffer
                        _src.Add(src.NodeName, src);
                    }
                    else if (row["category"].ToString() == NodeCategory.BILLER_ISSUER)
                    {
                        var snk = new SinkNode
                        {
                            NodeName = row["node_name"].ToString(),
                            InstID = row["inst_id"].ToString(),
                            Port = System.Convert.ToInt32(row["port_out"]),
                            AutoReversal = row["auto_reversal"].ToString(),
                            RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                            AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                            MaxRetrySend = Convert.ToInt16(row["saf_limit"])
                        };

                        await snk.Start();

                        //add to buffer
                        _snk.Add(snk.NodeName, snk);
                    }
                    else
                    {
                        var src = new SourceNode
                        {
                            NodeName = row["node_name"].ToString(),
                            InstID = row["inst_id"].ToString(),
                            Port = System.Convert.ToInt32(row["port_in"]),
                            AutoReversal = row["auto_reversal"].ToString(),
                            RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                            AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                            MaxRetrySend = Convert.ToInt16(row["saf_limit"])
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
                            RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                            AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                            MaxRetrySend = Convert.ToInt16(row["saf_limit"])
                        };

                        await snk.Start();

                        //add to buffer
                        _snk.Add(snk.NodeName, snk);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        public async Task Stop()
        {
            foreach (string key in _snk.Keys)
            {
                if (_src.ContainsKey(key) == true)
                    await _src[key].Stop();

                if (_snk.ContainsKey(key) == true)
                    await _snk[key].Stop();
            }

            _src.Clear();
            _snk.Clear();
        }

        public async Task Resync(string AppName)
        {
            await CheckNewNode(AppName);
            await CheckNodeDelete(AppName);
            await CheckNodeChange(AppName);
        }

        public async Task SendToSource(string NodeName, Message.Request MsgRequest)
        {
            if (_src.ContainsKey(NodeName) == true)
                await _src[NodeName].Send(MsgRequest);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        public async Task SendToSink(string NodeName, Message.Response MsgResponse)
        {
            if (_snk.ContainsKey(NodeName) == true)
                await _snk[NodeName].Reply(MsgResponse);
            else
                await _logger.LogAsync($"Node {NodeName} not found");
        }

        private async Task CheckNewNode(string AppName)
        {
            try
            {
                DataTable tbl = await _dbMgr.GetNodes(AppName);

                foreach (DataRow row in tbl.Rows)
                {
                    string node_name = row["node_name"].ToString();

                    if (row["category"].ToString() == NodeCategory.MERCHANT)
                    {
                        bool bFound = _src.ContainsKey(node_name);

                        if (bFound == false)
                        {
                            var src = new SourceNode
                            {
                                NodeName = row["node_name"].ToString(),
                                InstID = row["inst_id"].ToString(),
                                Port = System.Convert.ToInt32(row["port_in"]),
                                AutoReversal = row["auto_reversal"].ToString(),
                                RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                                AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                                MaxRetrySend = Convert.ToInt16(row["saf_limit"])
                            };

                            await src.Start();

                            //add to buffer
                            _src.Add(src.NodeName, src);
                        }
                    }
                    else if (row["category"].ToString() == NodeCategory.BILLER_ISSUER)
                    {
                        bool bFound = _snk.ContainsKey(node_name);

                        if (bFound == false)
                        {
                            var snk = new SinkNode
                            {
                                NodeName = row["node_name"].ToString(),
                                InstID = row["inst_id"].ToString(),
                                Port = System.Convert.ToInt32(row["port_out"]),
                                AutoReversal = row["auto_reversal"].ToString(),
                                RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                                AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                                MaxRetrySend = Convert.ToInt16(row["saf_limit"])
                            };

                            await snk.Start();

                            //add to buffer
                            _snk.Add(snk.NodeName, snk);
                        }
                    }
                    else
                    {
                        bool bFound = _src.ContainsKey(node_name);

                        if (bFound == false)
                        {
                            var src = new SourceNode
                            {
                                NodeName = row["node_name"].ToString(),
                                InstID = row["inst_id"].ToString(),
                                Port = System.Convert.ToInt32(row["port_in"]),
                                AutoReversal = row["auto_reversal"].ToString(),
                                RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                                AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                                MaxRetrySend = Convert.ToInt16(row["saf_limit"])
                            };

                            await src.Start();

                            //add to buffer
                            _src.Add(src.NodeName, src);
                        }

                        bFound = _snk.ContainsKey(node_name);

                        if (bFound == false)
                        {
                            var snk = new SinkNode
                            {
                                NodeName = row["node_name"].ToString(),
                                InstID = row["inst_id"].ToString(),
                                Port = System.Convert.ToInt32(row["port_out"]),
                                AutoReversal = row["auto_reversal"].ToString(),
                                RequestTimeout = Convert.ToInt16(row["request_timeout"]),
                                AdviceTimeout = Convert.ToInt16(row["advice_timeout"]),
                                MaxRetrySend = Convert.ToInt16(row["saf_limit"])
                            };

                            await snk.Start();

                            //add to buffer
                            _snk.Add(snk.NodeName, snk);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        private async Task CheckNodeDelete(string AppName)
        {
            try
            {
                DataTable tbl = await _dbMgr.GetNodes(AppName);
                bool bFound;

                //source node
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

                        //remove
                        _src.Remove(key);
                    }
                }

                //sink node
                foreach (string key in _snk.Keys)
                {
                    bFound = false;

                    foreach (DataRow row in tbl.Rows)
                    {
                        string node_name = row["node_name"].ToString();

                        if (node_name == _snk[key].NodeName)
                        {
                            bFound = true;
                            break;
                        }
                    }

                    if (bFound == false)
                    {
                        //stop node
                        await _snk[key].Stop();

                        //remove
                        _snk.Remove(key);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex.Message);
            }
        }

        private async Task CheckNodeChange(string AppName)
        {
            try
            {
                DataTable tbl = await _dbMgr.GetNodes(AppName);
                bool bRestart = false;

                foreach (DataRow row in tbl.Rows)
                {
                    string key = row["node_name"].ToString();
                    string inst_id = row["inst_id"].ToString();
                    string auto_rev = row["auto_reversal"].ToString();

                    int port_in = Convert.ToInt32(row["port_in"]);
                    int port_out = Convert.ToInt32(row["port_out"]);
                    int req_timeout = Convert.ToInt16(row["request_timeout"]);
                    int adv_timeout = Convert.ToInt16(row["advice_timeout"]);

                    //source node
                    if (_src.ContainsKey(key) == true)
                    {
                        if (_src[key].Port != port_in)
                            bRestart = true;
                        else
                            bRestart = false;

                        _src[key].InstID = inst_id;
                        _src[key].Port = port_in;
                        _src[key].AutoReversal = auto_rev;
                        _src[key].RequestTimeout = req_timeout;
                        _src[key].AdviceTimeout = adv_timeout;
                        _src[key].MaxRetrySend = Convert.ToInt16(row["saf_limit"]);

                        if (bRestart == true)
                        {
                            await _src[key].Stop();
                            await _src[key].Start();
                        }
                    }

                    //sink node
                    if (_snk.ContainsKey(key) == true)
                    {
                        if (_snk[key].Port != port_out)
                            bRestart = true;
                        else
                            bRestart = false;

                        _snk[key].InstID = inst_id;
                        _snk[key].Port = port_out;
                        _snk[key].AutoReversal = auto_rev;
                        _snk[key].RequestTimeout = req_timeout;
                        _snk[key].AdviceTimeout = adv_timeout;
                        _snk[key].MaxRetrySend = Convert.ToInt16(row["saf_limit"]);

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
                await _logger.LogAsync(ex.Message);
            }
        }
    }
}
