using ApiBiller.Models;
using Newtonsoft.Json;
using SyncNet.Message;

namespace ApiBiller.Message
{
    class ToRemote
    {
        private static string DE_REQUEST = "request";

        public static TransactionModel.Request Inquiry(Request req)
        {
            //data from channel
            var de = GetDataElementRequest(req);

            var model = new TransactionModel.Request
            {
                productId = req.receiving_inst_id,
                customerId = req.to_acc_number,
                amount = (long) req.amount_tran,
                refnum = req.refnum
            };

            return model;
        }

        public static TransactionModel.Request Payment(Request req)
        {
            //data from channel
            var de = GetDataElementRequest(req);

            var model = new TransactionModel.Request
            {
                productId = req.receiving_inst_id,
                customerId = req.to_acc_number,
                amount = (long)req.amount_tran,
                refnum = req.refnum
            };

            return model;
        }

        public static TransactionModel.Request Advice(Request req)
        {
            //data from channel
            var de = GetDataElementRequest(req);

            var model = new TransactionModel.Request
            {
                productId = req.receiving_inst_id,
                customerId = req.to_acc_number,
                amount = (long)req.amount_tran,
                refnum = req.refnum
            };

            return model;
        }

        public static TransactionModel.Request Reversal(Request req)
        {
            //data from channel
            var de = GetDataElementRequest(req);

            var model = new TransactionModel.Request
            {
                productId = req.receiving_inst_id,
                customerId = req.to_acc_number,
                amount = (long)req.amount_tran,
                refnum = req.refnum
            };

            return model;
        }

        #region Private Method
        private static DataElementModel.Request GetDataElementRequest(Request req)
        {
            var retval = new DataElementModel.Request();

            try
            {
                if (req.additional_data.TryGetValue(DE_REQUEST, out object value) == true)
                {
                    retval = JsonConvert.DeserializeObject<DataElementModel.Request>(value.ToString());
                }
            }
            catch { }

            return retval;
        }
        #endregion
    }
}
