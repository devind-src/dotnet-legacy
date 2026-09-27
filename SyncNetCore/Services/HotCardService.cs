using SyncNet.DbRepository;
using SyncNet.Models.Common;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SyncNet.Services
{
    class HotCardService
    {
        private readonly Dictionary<string, HotcardModel> _hotcard = [];

        public async Task Resync()
        {
            //clear buffer
            _hotcard.Clear();

            DataTable tbl = await DbMgr.GetHotcard();
            foreach (DataRow row in tbl.Rows)
            {
                HotcardModel h = new HotcardModel();
                //h.resp_code = row["resp_code"].ToString();
                //h.auth_resp = row["auth_id_resp"].ToString();

                string card_nr = row["card_nr"].ToString();

                //add to buffer
                if (_hotcard.ContainsKey(card_nr) == false)
                    _hotcard.Add(card_nr, h);
            }
        }

        public bool IsHotCard(string pan)
        {
            if (string.IsNullOrEmpty(pan) == true) return false;

            return _hotcard.TryGetValue(pan, out HotcardModel h);
        }
    }
}
