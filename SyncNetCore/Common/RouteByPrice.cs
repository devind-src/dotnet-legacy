using ARNCoreLinux.Library;
using ARNCoreLinux.Utils;
using System.Collections.Generic;
using System.Data;

namespace ARNCoreLinux.Common
{
    class RouteByPrice
    {
        private Dictionary<string, Models.TelcoPriceMerchant> _salesPrice;
        private DataTable _basicPrice;

        public RouteByPrice()
        {
            _salesPrice = new Dictionary<string, Models.TelcoPriceMerchant>();
            _basicPrice = new DataTable();
        }

        public void Resync()
        {
            //clear buffer
            _salesPrice.Clear();

            LoadProductPrice();
        }

        public bool TryGetRouting(ref Models.Request req)
        {
            string supplier_id = string.Empty;

            int denom = (int) req.AmountTran;
            int harga_beli = 0;
            int harga_jual = 0;

            //margin harga termurah
            int margin = getMarginLowestPrice(req, ref supplier_id, ref harga_beli, ref harga_jual);

            //validate margin
            if (margin <= 0) return false;

            //replace routing
            req.PrivateData.DestNode = supplier_id;

            //assign data
            req.PrivateData.MarginData.Denom = denom;
            req.PrivateData.MarginData.PurchasePrice = harga_beli;
            req.PrivateData.MarginData.SalesPrice = harga_jual;

            return true;
        }

        private int getMarginLowestPrice(Models.Request req, ref string supplier_id, ref int harga_beli, ref int harga_jual)
        {
            //biller code & denom
            string merchant_id = req.MerchantID;
            string biller_code = req.ProductID;

            int denom = (int) req.AmountTran; ;

            //search by biller_code + denom
            string expression = $"biller_code='{biller_code}' AND denom='{denom}'";
            string sortOrder = "harga_beli ASC";

            //get lower basic price from supplier for routing
            DataRow[] foundRows = _basicPrice.Select(expression, sortOrder);
            if (foundRows.Length == 0) return -1;

            //assign data
            supplier_id = foundRows[0]["supplier_id"].ToString();

            //assign data
            harga_beli = NbConvert.ToInt(foundRows[0]["harga_beli"].ToString());
            harga_jual = NbConvert.ToInt(foundRows[0]["harga_jual"].ToString());

            //composite key
            string key1 = merchant_id + biller_code + denom;

            //check price per merchant
            Models.TelcoPriceMerchant m;
            if (_salesPrice.TryGetValue(key1, out m) == true)
            {
                //replace with new data
                harga_jual = m.harga_jual;
            }

            //calculate margin
            int margin = harga_jual - harga_beli;

            return margin;
        }

        private void LoadProductPrice()
        {
            //price merchant
            string query = @"SELECT * FROM arr_product_price_merchant WHERE status='1' ";
            DataTable tbl = DbSql.getRecords(query);
            foreach (DataRow rec in tbl.Rows)
            {
                Models.TelcoPriceMerchant obj = new Models.TelcoPriceMerchant();
                obj.merchant_id = rec["merchant_id"].ToString();
                obj.product_name = rec["product_name"].ToString();
                obj.biller_code = rec["biller_code"].ToString();
                obj.denom = NbConvert.ToInt(rec["denom"].ToString());
                obj.harga_jual = NbConvert.ToInt(rec["harga_jual"].ToString());

                //add price
                if (_salesPrice.ContainsKey(obj.merchant_id + obj.biller_code + obj.denom) == false)
                    _salesPrice.Add(obj.merchant_id + obj.biller_code + obj.denom, obj);
            }

            //price supplier
            query = @"SELECT * FROM arr_product_price_supplier";
            _basicPrice = DbSql.getRecords(query);
        }
    }
}
