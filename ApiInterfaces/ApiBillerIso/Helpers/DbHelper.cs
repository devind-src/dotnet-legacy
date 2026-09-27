using SyncNet.Constants;
using SyncNet.DbRepository;
using System.Data;
using System.Threading.Tasks;

namespace ApiBiller.Helpers
{
    internal class DbHelper
    {
        private static readonly DbMgr _dbMgr = new();

        public static string GetBankNameByCBC(string bankCode)
        {
            string sqltext = $@"SELECT bank_name FROM sw_product_transfer 
                WHERE bank_code = @bank_code LIMIT 1";

            return _dbMgr.GetFieldValue(sqltext, new { bank_code = bankCode });
        }
        public static (string cbc, string bank_name) GetBankNameByPAN(string pan)
        {
            string cbc = "";
            string bank_name = "";

            string sqltext = $@"SELECT cbc,bank_name FROM sw_product_bins 
                WHERE '{pan}' LIKE bin || '%' LIMIT 1";

            DataRow rec = _dbMgr.GetRow(sqltext);
            if (rec != null)
            {
                cbc = rec["cbc"].ToString();
                bank_name = rec["bank_name"].ToString();
            }

            return (cbc, bank_name);
        }
    }
}
