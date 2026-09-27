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

                case "01":
                    errmsg = "Kendala pada kartu";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "02":
                    errmsg = "Kendala spesifik pada kartu";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "05":
                    errmsg = "Transaksi ditolak";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "12":
                    errmsg = "Transaksi tidak valid";
                    action = "Hubungi helpdesk";
                    break;
                case "13":
                    errmsg = "Nominal transaksi tidak valid";
                    action = "Silahkan transaksi kembali dengan nominal berbeda";
                    break;
                case "14":
                    errmsg = "Transaksi ditolak";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "30":
                    errmsg = "Format message tidak valid";
                    action = "Hubungi helpdesk";
                    break;
                case "38":
                    errmsg = "PIN salah melebihi limit";
                    break;
                case "41":
                    errmsg = "Kartu hilang";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "43":
                    errmsg = "Kartu curian";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "51":
                    errmsg = "Saldo tidak cukup";
                    break;
                case "54":
                    errmsg = "Masa berlaku kartu habis";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "55":
                    errmsg = "PIN yang anda masukan salah";
                    break;
                case "57":
                    errmsg = "Kendala pada kartu";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "61":
                    errmsg = "Transaksi melebihi limit";
                    break;
                case "62":
                    errmsg = "Kartu diblokir";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "65":
                    errmsg = "Transaksi melebihi limit";
                    break;
                case "68":
                    errmsg = "Transaksi tertunda";
                    action = "Silahkan cek riwayat transaksi";
                    break;
                case "75":
                    errmsg = "PIN salah melebihi batas";
                    break;
                case "76":
                    errmsg = "No rekening tidak valid";
                    action = "Silahkan periksa kembali";
                    break;
                case "78":
                    errmsg = "Rekening ditutup";
                    action = "Hubungi bank penerbit kartu";
                    break;
                case "81":
                    errmsg = "PIN block tidak valid";
                    action = "Hubungi helpdesk";
                    break;
                case "87":
                    errmsg = "PIN error";
                    action = "Hubungi helpdesk";
                    break;
                case "90":
                    errmsg = "Sistem sedang proses akhir hari";
                    action = "Silahkan transaksi kembali setelah beberapa saat";
                    break;
                case "91":
                    errmsg = "Gangguan pada komunikasi";
                    action = "Hubungi helpdesk";
                    break;
                case "92":
                    errmsg = "Gangguan pada jalur transaksi";
                    action = "Hubungi helpdesk";
                    break;
                case "94":
                    errmsg = "Duplikat transaksi";
                    action = "Silahkan transaksi kembali";
                    break;
                case "96":
                    errmsg = "Terjadi gangguan teknis pada server";
                    action = "Silahkan transaksi kembali setelah beberapa saat";
                    break;
                default:
                    errmsg = "";
                    break;
            }

            //set return value
            string retval = errmsg;
            if (!string.IsNullOrEmpty(action)) retval = errmsg + ". " + action;

            return retval;
        }

        public static string GetRespMessageEN(string rc_nobu)
        {
            string retval;

            switch (rc_nobu)
            {
                case "00":
                    retval = "SUCCESS";
                    break;

                case "01":
                    retval = "REFER TO CARD ISSUER";
                    break;
                case "02":
                    retval = "REFER TO SPECIAL CONDITIONS FOR CARD ISSUER";
                    break;
                case "05":
                    retval = "DO NOT APPROVE";
                    break;
                case "12":
                    retval = "INVALID TRANSACTION";
                    break;
                case "13":
                    retval = "INVALID AMOUNT";
                    break;
                case "14":
                    retval = "INVALID CARD NUMBER";
                    break;
                case "30":
                    retval = "FORMAT ERROR";
                    break;
                case "38":
                    retval = "PIN TRIES EXCEEDED";
                    break;
                case "41":
                    retval = "LOST CARD";
                    break;
                case "43":
                    retval = "STOLEN CARD";
                    break;
                case "51":
                    retval = "INSUFICIENT FUNDS";
                    break;
                case "54":
                    retval = "EXPIRED CARD";
                    break;
                case "55":
                    retval = "INCORRECT PIN";
                    break;
                case "57":
                    retval = "TXN NOT PERMITTED TO CARDHOLDER";
                    break;
                case "61":
                    retval = "EXCEEDS TRANSACTION LIMIT";
                    break;
                case "62":
                    retval = "RESTIRTCTED CARD";
                    break;
                case "65":
                    retval = "EXCEED TRANSACTION FREQUENCY LIMIT";
                    break;
                case "68":
                    retval = "TIMEOUT";
                    break;
                case "75":
                    retval = "PIN TRIES EXCEED";
                    break;
                case "76":
                    retval = "INVALID ACCOUNT";
                    break;
                case "78":
                    retval = "CLOSED ACCOUNT";
                    break;
                case "81":
                    retval = "INVALID PINBLOCK";
                    break;
                case "87":
                    retval = "PIN KEY ERROR";
                    break;
                case "90":
                    retval = "CUT OFF IN PROGRESS";
                    break;
                case "91":
                    retval = "ISSUER OR SWITCH NOT AVAILABLE";
                    break;
                case "92":
                    retval = "UNABLE TO ROUTE";
                    break;
                case "94":
                    retval = "DUPLICATE TRANSMISSION";
                    break;
                case "96":
                    retval = "SYSTEM MALFUNCTION";
                    break;
                default:
                    retval = "";
                    break;
            }

            return retval;
        }
    }
}
