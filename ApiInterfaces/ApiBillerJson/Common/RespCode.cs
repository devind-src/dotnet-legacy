namespace ApiBiller.Common
{
    internal class RespCode
    {
        public static string GetRCMapping(string rc_host)
        {
            string retval = rc_host;

            //TODO: jika ada rc yang perlu di mapping           

            return retval;
        }
        public static string GetRespMessage(string rc)
        {
            string errmsg;
            string action = "";

            switch (rc)
            {
                case "00":
                    errmsg = "Transaksi berhasil";
                    break;

                case "12":
                    errmsg = "Transaksi tidak valid";
                    action = "Hubungi helpdesk";
                    break;

                //TODO: jika ada rc lainnya yang perlu di mapping


                default:
                    errmsg = "";
                    break;
            }

            //set return value
            string retval = errmsg;
            if (!string.IsNullOrEmpty(action)) retval = errmsg + ". " + action;

            return retval;
        }
    }
}
