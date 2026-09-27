using SWTCoreLab.DbEngine;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SWTCoreLab.Cams
{
    class BINObj
    {
        public string issuer;
        public string prefix;

        public BINObj()
        {
            issuer = string.Empty;
            prefix = string.Empty;
        }
    }

    class BIN
    {
        private readonly List<BINObj> _list_bins;

        public BIN()
        {
            _list_bins = new List<BINObj>();
        }

        public async Task Resync()
        {
            _list_bins.Clear();

            string query = "SELECT issuer,pan_prefix FROM cms_products";
            DataTable tbl = await DbMgr.getRecords(query);

            foreach (DataRow record in tbl.Rows)
            {
                BINObj obj = new BINObj
                {
                    issuer = record["issuer"].ToString(),
                    prefix = record["pan_prefix"].ToString()
                };

                _list_bins.Add(obj);
            }
        }

        public int CheckBin(string issuer, string pan)
        {
            int len;
            int ret = -1; //default is not found

            for (int i = 0; i < _list_bins.Count; i++)
            {
                len = _list_bins[i].prefix.Length;

                if (_list_bins[i].issuer == issuer &&
                    _list_bins[i].prefix == pan.Substring(0, len))
                {
                    ret = 0;
                    break;
                }
            }

            return ret;
        }
    }
}
