using SWTSdk.DbEngine;
using System.Collections.Generic;
using System.Data;

namespace APITerminal.Common
{
    internal class BankName
    {
        private static readonly Dictionary<string, string> _bank = [];

        public static void Initialize()
        {
            _bank.Clear();

            string sqltext = $@"SELECT product_code,product_name FROM sw_product";

            DataTable tbl = DbPgSql.getRecords(sqltext);

            foreach (DataRow rec in tbl.Rows)
            {
                string cbc = rec["product_code"].ToString();
                string name = rec["product_name"].ToString();

                if (_bank.ContainsKey(cbc) == false)
                    _bank.Add(cbc, name);
            }
        }

        public static string GetBankName(string bankCode)
        {
            string ret;

            //validate input
            if (string.IsNullOrEmpty(bankCode) == true) return bankCode;

            if (AppParams.DbMemory == true)
                ret = GetBankNameMem(bankCode);
            else
                ret = GetBankNameDb(bankCode);

            //validate response
            if (string.IsNullOrEmpty(ret) == true) ret = bankCode;

            return ret;
        }

        private static string GetBankNameMem(string bankCode)
        {
            string ret = "";

            if (_bank.TryGetValue(bankCode, out var bankName) == true)
                ret = bankName;

            return ret;
        }

        private static string GetBankNameDb(string bankCode)
        {
            string sqltext = $@"SELECT product_name FROM sw_product 
                WHERE product_code='{bankCode.Sanitize()}'";

            return DbPgSql.getFieldValue(sqltext);
        }
    }
}
