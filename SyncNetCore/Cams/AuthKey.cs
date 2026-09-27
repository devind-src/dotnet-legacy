using SWTCoreLab.DbEngine;
using SWTCoreLab.Library;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SWTCoreLab.Cams
{
    class NodeKeyObj
    {
        public string node_name;
        public string issuer;
        public string master_key;
        public string key_under_zmk;
        public string key_check_value;

        public NodeKeyObj()
        {
            node_name = string.Empty;
            issuer = string.Empty;
            master_key = string.Empty;
            key_under_zmk = string.Empty;
            key_check_value = string.Empty;
        }
    }

    class IssuerKeyObj
    {
        public string issuer;
        public string master_key;
        public string key_under_lmk;

        public IssuerKeyObj()
        {
            issuer = string.Empty;
            master_key = string.Empty;
            key_under_lmk = string.Empty;
        }
    }

    class AuthKey
    {
        public const string PARRENTKEY = "96CCE365CD20047CA2E90FA065AF7021A1F423FBC2332D5B";

        private readonly Dictionary<string, IssuerKeyObj> _list_issuer_key;
        private readonly Dictionary<string, NodeKeyObj> _list_node;

        public AuthKey()
        {
            _list_issuer_key = new Dictionary<string, IssuerKeyObj>();
            _list_node = new Dictionary<string, NodeKeyObj>();
        }

        public async Task Resync()
        {
            //clear array
            _list_issuer_key.Clear();
            _list_node.Clear();

            string queryCommand = "SELECT * FROM cms_issuer_keys";
            DataTable tbl = await DbMgr.getRecords(queryCommand);

            foreach (DataRow row in tbl.Rows)
            {
                IssuerKeyObj obj = new IssuerKeyObj
                {
                    issuer = row["issuer"].ToString(),
                    master_key = row["master_key"].ToString(),
                    key_under_lmk = row["key_under_lmk"].ToString()
                };

                //add to buffer
                if (_list_issuer_key.ContainsKey(obj.issuer) == false)
                    _list_issuer_key.Add(obj.issuer, obj);
            }

            queryCommand = @"SELECT sw_nodes.node_name,sw_nodes.issuer, 
                sw_crypto_keys.master_key,sw_crypto_keys.key_under_zmk,
                sw_crypto_keys.key_check_value 
                FROM sw_crypto_keys 
                INNER JOIN sw_nodes on sw_nodes.node_id=sw_crypto_keys.node_id 
                WHERE provider_service='1'";

            tbl = await DbMgr.getRecords(queryCommand);

            foreach (DataRow row in tbl.Rows)
            {
                NodeKeyObj obj = new NodeKeyObj
                {
                    node_name = row["node_name"].ToString(),
                    issuer = row["issuer"].ToString(),
                    master_key = row["master_key"].ToString(),
                    key_under_zmk = row["key_under_zmk"].ToString(),
                    key_check_value = row["key_check_value"].ToString()
                };

                string key = obj.node_name + obj.issuer;

                //add to buffer
                if (_list_node.ContainsKey(key) == false)
                    _list_node.Add(key, obj);
            }
        }

        public string getIssuerKey(string node_name, string issuer)
        {
            string retval = string.Empty;
            string clear_key = string.Empty;

            NbDes d = new NbDes();

            //check issuer
            if (_list_issuer_key.TryGetValue(issuer, out IssuerKeyObj obj) == true)
            {
                //get clear mk                    
                string clear_mk = NbDes.DecryptHex(obj.master_key, PARRENTKEY);

                //get clear key
                clear_key = NbDes.DecryptHex(obj.key_under_lmk, clear_mk);
            }

            //check node
            string key = node_name + issuer;
            if (_list_node.TryGetValue(key, out NodeKeyObj node) == true)
            {
                retval = NbDes.DecryptHex(node.key_under_zmk, clear_key);
            }

            return retval;
        }
    }
}
