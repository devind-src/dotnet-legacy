using SWTCoreLab.DbEngine;
using SWTCoreLab.Library;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SWTCoreLab.Cams
{
    class IssuerObj
    {
        public string issuer;
        public string auth_service;
        public string velocity_per_tran;
        public string velocity_daily;
        public string velocity_weekly;
        public string velocity_monthly;

        public IssuerObj()
        {
            issuer = string.Empty;
            auth_service = string.Empty;
            velocity_per_tran = string.Empty;
            velocity_daily = string.Empty;
            velocity_weekly = string.Empty;
            velocity_monthly = string.Empty;
        }
    }

    class Issuer
    {
        private readonly Dictionary<string, IssuerObj> _list_issuer;

        public Issuer()
        {
            _list_issuer = new Dictionary<string, IssuerObj>();
        }

        public async Task Resync()
        {
            _list_issuer.Clear();

            string query = "SELECT * FROM cms_issuers";
            DataTable tbl = await DbMgr.getRecords(query);
            foreach (DataRow row in tbl.Rows)
            {
                IssuerObj obj = new IssuerObj
                {
                    issuer = row["issuer"].ToString(),
                    auth_service = row["auth_service"].ToString(),
                    velocity_per_tran = row["velocity_per_tran"].ToString(),
                    velocity_daily = row["velocity_daily"].ToString(),
                    velocity_weekly = row["velocity_weekly"].ToString(),
                    velocity_monthly = row["velocity_monthly"].ToString()
                };

                //add to buffer
                if (_list_issuer.ContainsKey(obj.issuer) == false)
                    _list_issuer.Add(obj.issuer, obj);
            }
        }

        public int getIssuerAuthService(string issuer)
        {
            int ret = 0; //default No Stand-in

            if (_list_issuer.TryGetValue(issuer, out IssuerObj obj) == true)
                ret = NbConvert.ToInt(obj.auth_service);

            return ret;
        }

        public int getVelocityPerTran(string issuer)
        {
            int ret = 0;

            if (_list_issuer.TryGetValue(issuer, out IssuerObj obj) == true)
                ret = NbConvert.ToInt(obj.velocity_per_tran);

            return ret;
        }

        public int getVelocityDaily(string issuer)
        {
            int ret = 0;

            if (_list_issuer.TryGetValue(issuer, out IssuerObj obj) == true)
                ret = NbConvert.ToInt(obj.velocity_daily);

            return ret;
        }

        public int getVelocityWeekly(string issuer)
        {
            int ret = 0;

            if (_list_issuer.TryGetValue(issuer, out IssuerObj obj) == true)
                ret = NbConvert.ToInt(obj.velocity_weekly);

            return ret;
        }

        public int getVelocityMonthly(string issuer)
        {
            int ret = 0;

            if (_list_issuer.TryGetValue(issuer, out IssuerObj obj) == true)
                ret = NbConvert.ToInt(obj.velocity_monthly);

            return ret;
        }

        #region get directly from database without resync
        public async Task<int> getVelocityPerTranDb(string issuer)
        {
            return NbConvert.ToInt(await DbMgr.getFieldValue($"SELECT velocity_per_tran FROM cms_issuers WHERE issuer='{issuer}'"));
        }

        public async Task<int> getVelocityDailyDb(string issuer)
        {
            return NbConvert.ToInt(await DbMgr.getFieldValue($"SELECT velocity_daily FROM cms_issuers WHERE issuer='{issuer}'"));
        }

        public async Task<int> getVelocityWeeklyDb(string issuer)
        {
            return NbConvert.ToInt(await DbMgr.getFieldValue($"SELECT velocity_weekly FROM cms_issuers WHERE issuer='{issuer}'"));
        }

        public async Task<int> getVelocityMonthlyDb(string issuer)
        {
            return NbConvert.ToInt(await DbMgr.getFieldValue($"SELECT velocity_monthly FROM cms_issuers WHERE issuer='{issuer}'"));
        }
        #endregion
    }
}
