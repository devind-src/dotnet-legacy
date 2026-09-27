using ApiChannel.Models.Channel;
using Newtonsoft.Json;

namespace ApiChannel.Library
{
    internal class RespCode
    {
        public const string SUCCESS = "00:SUCCESS";

        public const string TERMINAL_DOES_NOT_EXIST = "X1:TERMINAL BELUM TERDAFTAR";
        public const string DUPLICATE_TRANSACTION = "X2:TRANSAKSI DUPLIKAT";
        public const string INVALID_TRANSACTION = "X3:TIPE TRANSAKSI TIDAK TERDAFTAR";
        public const string INVALID_MSGTYPE = "X4:TIPE MESSAGE TIDAK VALID";
        public const string INVALID_PINBLOCK = "X5:PINBLOCK TIDAK VALID";
        public const string INVALID_URL = "X6:URL TIDAK VALID";
        public const string SIGNON_FAILED = "X7:SIGN ON GAGAL";
        public const string AUTH_FAILED = "X8:OTENTIKASI TIDAK VALID";
        public const string WDL_DENIED = "X9:NOREK WDL TIDAK TERDAFTAR";
        public const string INVALID_SERIAL_NUMBER = "X10:SERIAL NUMBER TIDAK VALID";
        public const string MAX_TRANSFER_EXCEED = "X11:MAKSIMUM TRANSFER RP {nominal}";
        public const string MAX_WITHDRAWAL_EXCEED = "X12:MAKSIMUM TARIK TUNAI RP {nominal}";
        public const string MAX_PURCHASE_EXCEED = "X13:MAKSIMUM PEMBELIAN RP {nominal}";
        public const string MERCHANT_NOT_ACTIVE = "X14:MERCHANT TIDAK AKTIF";
        public const string BILLER_CUTOFF = "X15:BILLER SEDANG CUT-OFF"; //Jadwal Routing: tidak ada biller yang buka

        public const string INVALID_ICC_DATA = "C1:ICC DATA TIDAK VALID";

        public static BaseResponse GetObject(string code)
        {
            string[] arr = code.Split(':');

            BaseResponse b = new BaseResponse();
            b.responseStatus = arr[0];
            b.responseMessage = arr[1];

            return b;
        }

        public static string GetString(string code)
        {
            string[] arr = code.Split(':');

            BaseResponse b = new BaseResponse();
            b.responseStatus = arr[0];
            b.responseMessage = arr[1];

            return JsonConvert.SerializeObject(b);
        }
    }
}
