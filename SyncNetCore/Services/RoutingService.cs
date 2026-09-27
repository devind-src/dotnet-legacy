using SyncNet.DbRepository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    class BIN
    {
        public string NodeName;
        public string BinNumber;
    }

    class RoutingService
    {
        private readonly List<BIN> _routeByBIN = [];

        private readonly Dictionary<string, string> _routeByCluster = [];
        private readonly Dictionary<string, string> _routeBySource = [];
        private readonly Dictionary<string, string> _routeBySink = [];
        private readonly Dictionary<string, string> _routeBySourceDefault = [];
        private readonly Dictionary<string, string> _routeByProduct = [];

        public async Task Resync()
        {
            //LoadRouteByCluster();
            await LoadRouteByBIN();
            await LoadRouteBySource();
            await LoadRouteBySink();
            await LoadRouteByProduct();
        }

        public async Task<(bool Success, string DestNode)> TryGetRouting(Message.Request req)
        {
            //push route
            if (string.IsNullOrEmpty(req.private_data.sink_node) == false)
            {
                if (_routeBySink.ContainsKey(req.private_data.sink_node) == true)
                {
                    //route by interface
                    return (true, req.private_data.sink_node);
                }
            }

            //route by source
            string dest_node = await GetNodeBySource(req);

            //route by product
            if (string.IsNullOrEmpty(dest_node) == true && req.receiving_inst_id != null)
                dest_node = await GetNodeByProduct(req.receiving_inst_id);

            //route by BIN
            //if (string.IsNullOrEmpty(dest_node) == true && req.pan != null)
            if (string.IsNullOrEmpty(dest_node) == true && req.pan != null && req.receiving_inst_id == null)
                dest_node = await GetNodeByBIN(req.pan);

            //result
            bool  bval = !string.IsNullOrEmpty(dest_node);

            return (bval, dest_node);
        }

        private string GetNodeByCluster(string cid, string product)
        {
            string node_id = string.Empty;
            string key = cid + product;

            if (_routeByCluster.TryGetValue(key, out node_id) == false)
                node_id = string.Empty;

            return node_id;
        }

        private async Task<string> GetNodeBySource(Message.Request req)
        {
            string ret = string.Empty;

            try
            {
                string source_node = req.private_data.source_node;
                string dest_node = string.Empty;

                //try get route by source + product id
                string key = source_node + req.receiving_inst_id;

                if (_routeBySource.TryGetValue(key, out dest_node) == true)
                {
                    ret = dest_node;
                }
                else
                {
                    //try get route only source
                    if (_routeBySourceDefault.TryGetValue(source_node, out dest_node) == true)
                    {
                        ret = dest_node;
                    }
                }
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }

            return ret;
        }

        private async Task<string> GetNodeByProduct(string ProductID)
        {
            string ret = string.Empty;

            try
            {
                string node = string.Empty;

                if (_routeByProduct.TryGetValue(ProductID, out node) == true)
                {
                    ret = node;
                }
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }

            return ret;
        }

        public async Task<string> GetNodeByBIN(string pan)
        {
            string ret = string.Empty;

            if (string.IsNullOrEmpty(pan) == true) return ret;

            try
            {
                for (int i = 0; i < _routeByBIN.Count; i++)
                {
                    if (_routeByBIN[i].BinNumber == pan.Substring(0, _routeByBIN[i].BinNumber.Length))
                    {
                        ret = _routeByBIN[i].NodeName;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                await MyApp.Logger(ex.Message);
            }

            return ret;
        }

        private async Task LoadRouteByCluster()
        {
            //clear buffer
            _routeByCluster.Clear();

            DataTable tbl = await DbMgr.GetRouteByCluster();
            foreach (DataRow rec in tbl.Rows)
            {
                string cid = rec["cid"].ToString();
                string product = rec["product"].ToString();
                string node_name = rec["node_name"].ToString();

                string key = cid + product;

                //make sure no duplicate
                if (_routeByCluster.ContainsKey(key) == false)
                {
                    //add to buffer
                    _routeByCluster.Add(key, node_name);
                }
            }
        }

        private async Task LoadRouteByBIN()
        {
            //clear buffer
            _routeByBIN.Clear();

            DataTable table = await DbMgr.GetRouteByBIN();
            foreach (DataRow record in table.Rows)
            {
                BIN obj = new BIN
                {
                    NodeName = record["node_name"].ToString(),
                    BinNumber = record["bin_nr"].ToString()
                };

                _routeByBIN.Add(obj);
            }
        }

        private async Task LoadRouteBySource()
        {
            //clear buffer
            _routeBySourceDefault.Clear();
            _routeBySource.Clear();

            DataTable tbl = await DbMgr.GetRouteBySource();
            foreach (DataRow row in tbl.Rows)
            {
                string source_node = row["node_in"].ToString();
                string dest_node = row["node_out"].ToString();
                string inst_id = row["inst_id"].ToString();

                //add to list
                if (string.IsNullOrEmpty(inst_id) == true)
                    _routeBySourceDefault.Add(source_node, dest_node);
                else
                {
                    string key = source_node + inst_id;
                    _routeBySource.Add(key, dest_node);
                }
            }
        }

        private async Task LoadRouteBySink()
        {
            //clear buffer
            _routeBySink.Clear();

            DataTable tbl = await DbMgr.GetRouteBySink();
            foreach (DataRow row in tbl.Rows)
            {
                string node_name = row["node_name"].ToString();

                //add to list
                _routeBySink.Add(node_name, "");
            }
        }

        private async Task LoadRouteByProduct()
        {
            //clear buffer
            _routeByProduct.Clear();

            DataTable tbl = await DbMgr.GetRouteByInst();
            foreach (DataRow row in tbl.Rows)
            {
                string inst_id = row["inst_id"].ToString();
                string node_name = row["node_name"].ToString();

                //add to list
                _routeByProduct.Add(inst_id, node_name);
            }
        }
    }
}
