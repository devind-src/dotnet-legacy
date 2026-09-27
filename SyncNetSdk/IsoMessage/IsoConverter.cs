using Newtonsoft.Json;
using SyncNet.Constants;
using SyncNet.Helpers;
using SyncNet.Library;
using SyncNet.Message;
using System.Collections.Generic;

namespace SyncNet.IsoMessage
{
    public class IsoConverter
    {
        private const string PrivateDataRequest = "request";
        private const string PrivateDataResponse = "response";

        public static Request GetSdkMsgRequest(Iso8583 iso)
        {
            if (iso == null) return null;

            string[] de_127 = NbMessage.ExtractXmlMessage(iso.GetField(127));

            var req = new Request();
            req.msgtype = iso.MsgType;
            req.pan = iso.GetField(2);
            req.tran_type = GetTranTypeStr(iso.MsgType, iso.GetField(3));
            req.tran_type_ext = GetTranType(iso.GetField(3));
            req.from_acc_type = GetFromAccType(iso.GetField(3));
            req.to_acc_type = GetToAccType(iso.GetField(3));
            req.amount_tran = NbConvert.ToLong(iso.GetField(4));
            req.datetime_tran = iso.GetField(7);
            req.trace_number = iso.GetField(11);
            req.merchant_type = iso.GetField(18);
            req.pos_entry_mode = iso.GetField(22);
            req.fee_data.biller_fee = NbConvert.ToInt(iso.GetField(28));
            req.fee_data.total_fee = NbConvert.ToInt(iso.GetField(29));
            req.fee_data.switch_fee = NbConvert.ToInt(iso.GetField(30));
            req.fee_data.acquirer_fee = NbConvert.ToInt(iso.GetField(31));
            req.acq_inst_id = iso.GetField(32);
            req.fwd_inst_id = iso.GetField(33);
            req.refnum = iso.GetField(37) ?? de_127[37];
            req.terminal_id = iso.GetField(41)?.Trim();
            req.merchant_id = iso.GetField(42)?.Trim();
            req.currency = iso.GetField(49);
            req.original_data = GetOrigData(iso.GetField(3), iso.GetField(56));
            req.echo_data = iso.GetField(59);
            req.virtual_account.acc_number = iso.GetField(98);
            req.receiving_inst_id = iso.GetField(100);
            req.from_acc_number = iso.GetField(102);
            req.to_acc_number = iso.GetField(103);

            req.security.pindata = iso.GetField(52);
            req.security.miscdata = iso.GetField(53);
            req.security.track2data = iso.GetField(35);
            req.security.iccdata = iso.GetField(55);

            //split trx credit DO NOT translate pinblock
            if (iso.GetField(128) == "02")
                req.security.is_debet_tran = false;
            else
                req.security.is_debet_tran = true;

            //set additional data
            var additionalData = new AdditionalData
            {
                de12 = iso.GetField(12),
                de13 = iso.GetField(13),
                de15 = iso.GetField(15),
                de24 = iso.GetField(24),
                de48 = iso.GetField(48),
                de56 = iso.GetField(56),
                de60 = iso.GetField(60),
                de61 = iso.GetField(61),
                de62 = iso.GetField(62),
                de63 = iso.GetField(63),
                de64 = iso.GetField(64),
                de65 = iso.GetField(65),

                de127 = de_127.ArrayToDict()
            };

            req.additional_data.Add(PrivateDataRequest, additionalData);

            return req;
        }
        public static Response GetSdkMsgResponse(Iso8583 iso)
        {
            if (iso == null) return null;

            string[] de_127 = NbMessage.ExtractXmlMessage(iso.GetField(127));

            var rsp = new Response();
            rsp.msgtype = iso.MsgType;
            rsp.pan = iso.GetField(2);
            rsp.tran_type = GetTranTypeStr(iso.MsgType, iso.GetField(3));
            rsp.tran_type_ext = GetTranType(iso.GetField(3));
            rsp.from_acc_type = GetFromAccType(iso.GetField(3));
            rsp.to_acc_type = GetToAccType(iso.GetField(3));
            rsp.amount_tran = NbConvert.ToLong(iso.GetField(4));
            rsp.datetime_tran = iso.GetField(7);
            rsp.trace_number = iso.GetField(11);
            rsp.merchant_type = iso.GetField(18);
            rsp.pos_entry_mode = iso.GetField(22);
            rsp.fee_data.biller_fee = NbConvert.ToInt(iso.GetField(28));
            rsp.fee_data.total_fee = NbConvert.ToInt(iso.GetField(29));
            rsp.fee_data.switch_fee = NbConvert.ToInt(iso.GetField(30));
            rsp.fee_data.acquirer_fee = NbConvert.ToInt(iso.GetField(31));
            rsp.acq_inst_id = iso.GetField(32);
            rsp.fwd_inst_id = iso.GetField(33);
            rsp.security.track2data = iso.GetField(35);
            rsp.refnum = iso.GetField(37) ?? de_127[37];
            rsp.resp_code = iso.GetField(39);
            rsp.terminal_id = iso.GetField(41)?.Trim();
            rsp.merchant_id = iso.GetField(42)?.Trim();
            rsp.currency = iso.GetField(49);
            rsp.additional_amount = iso.GetField(54);
            rsp.original_data = iso.GetField(56);
            rsp.echo_data = iso.GetField(59);
            rsp.virtual_account.acc_number = iso.GetField(98);
            rsp.receiving_inst_id = iso.GetField(100);
            rsp.from_acc_number = iso.GetField(102);
            rsp.to_acc_number = iso.GetField(103);

            rsp.security.pindata = iso.GetField(52);
            rsp.security.miscdata = iso.GetField(53);
            rsp.security.iccdata = iso.GetField(55);

            //set additional data
            var additionalData = new AdditionalData
            {
                de12 = iso.GetField(12),
                de13 = iso.GetField(13),
                de15 = iso.GetField(15),
                de24 = iso.GetField(24),
                de48 = iso.GetField(48),
                de56 = iso.GetField(56),
                de60 = iso.GetField(60),
                de61 = iso.GetField(61),
                de62 = iso.GetField(62),
                de63 = iso.GetField(63),
                de64 = iso.GetField(64),
                de65 = iso.GetField(65),

                de127 = de_127.ArrayToDict()
            };

            rsp.additional_data.Add(PrivateDataResponse, additionalData);

            return rsp;
        }

        public static Iso8583 GetIsoMessage(Request req)
        {
            if (req == null) return null;

            //convert to iso8583
            var iso = new Iso8583();
            iso.MsgType = req.msgtype;
            iso.PutField(2, req.pan);
            iso.PutField(3, GetPCode(req.tran_type, req.tran_type_ext, req.from_acc_type, req.to_acc_type));
            iso.PutField(4, GetAmount(req.amount_tran));
            iso.PutField(7, req.datetime_tran);
            iso.PutField(11, req.trace_number);
            iso.PutField(18, req.merchant_type);
            iso.PutField(22, req.pos_entry_mode);
            iso.PutField(28, req.fee_data.biller_fee.ToString().PadLeft(9, '0'));
            iso.PutField(29, req.fee_data.total_fee.ToString().PadLeft(9, '0'));
            iso.PutField(30, req.fee_data.switch_fee.ToString().PadLeft(9, '0'));
            iso.PutField(31, req.fee_data.acquirer_fee.ToString().PadLeft(9, '0'));
            iso.PutField(32, req.acq_inst_id);
            iso.PutField(33, req.fwd_inst_id);
            iso.PutField(35, req.security.track2data);
            iso.PutField(37, req.refnum);
            iso.PutField(41, req.terminal_id?.PadRight(16, ' '));
            iso.PutField(42, req.merchant_id?.PadRight(15, ' '));
            iso.PutField(50, req.currency);
            iso.PutField(52, req.security.pindata);
            iso.PutField(55, req.security.iccdata);
            iso.PutField(56, req.original_data);
            iso.PutField(59, req.echo_data?.ToString());
            iso.PutField(98, req.virtual_account.acc_number);
            iso.PutField(100, req.receiving_inst_id);
            iso.PutField(102, req.from_acc_number);
            iso.PutField(103, req.to_acc_number);

            //extract additional data element
            var additionalData = GetPrivateDataRequest(req.additional_data);
            string[] de_127 = additionalData.de127.DictToArray().ResizeArray(129);

            iso.PutField(12, additionalData.de12);
            iso.PutField(13, additionalData.de13);
            iso.PutField(15, additionalData.de15);
            iso.PutField(24, additionalData.de24);
            iso.PutField(48, additionalData.de48);
            iso.PutField(56, additionalData.de56);
            iso.PutField(60, additionalData.de60);
            iso.PutField(61, additionalData.de61);
            iso.PutField(62, additionalData.de62);
            iso.PutField(63, additionalData.de63);
            iso.PutField(64, additionalData.de64);
            iso.PutField(65, additionalData.de65);

            //copy additional data element
            iso.PutField(127, NbMessage.ContructXmlMessage(de_127));

            return iso;
        }
        public static Iso8583 GetIsoMessage(Response rsp)
        {
            if (rsp == null) return null;

            //convert to iso8583
            var iso = new Iso8583();
            iso.MsgType = NbMessage.GetMsgTypeResp(rsp.msgtype);
            iso.PutField(2, rsp.pan);
            iso.PutField(3, GetPCode(rsp.tran_type, rsp.tran_type_ext, rsp.from_acc_type, rsp.to_acc_type));
            iso.PutField(4, GetAmount(rsp.amount_tran));
            iso.PutField(7, rsp.datetime_tran);
            iso.PutField(11, rsp.trace_number);
            iso.PutField(18, rsp.merchant_type);
            iso.PutField(22, rsp.pos_entry_mode);
            iso.PutField(18, rsp.merchant_type);
            iso.PutField(28, rsp.fee_data.biller_fee.ToString().PadLeft(9, '0'));
            iso.PutField(29, rsp.fee_data.total_fee.ToString().PadLeft(9, '0'));
            iso.PutField(30, rsp.fee_data.switch_fee.ToString().PadLeft(9, '0'));
            iso.PutField(31, rsp.fee_data.acquirer_fee.ToString().PadLeft(9, '0'));
            iso.PutField(32, rsp.acq_inst_id);
            iso.PutField(33, rsp.fwd_inst_id);
            iso.PutField(35, rsp.security.track2data);
            iso.PutField(37, rsp.refnum);
            iso.PutField(39, rsp.resp_code);
            iso.PutField(41, rsp.terminal_id?.PadRight(16, ' '));
            iso.PutField(42, rsp.merchant_id?.PadRight(15, ' '));
            iso.PutField(50, rsp.currency);
            iso.PutField(54, rsp.additional_amount);
            iso.PutField(55, rsp.security.iccdata);
            iso.PutField(56, rsp.original_data);
            iso.PutField(98, rsp.virtual_account.acc_number);
            iso.PutField(100, rsp.receiving_inst_id);
            iso.PutField(102, rsp.from_acc_number);
            iso.PutField(103, rsp.to_acc_number);

            //extract additional data element
            var additionalData = GetPrivateDataResponse(rsp.additional_data);
            string[] de_127 = additionalData.de127.DictToArray().ResizeArray(129);

            iso.PutField(12, additionalData.de12);
            iso.PutField(13, additionalData.de13);
            iso.PutField(15, additionalData.de15);
            iso.PutField(24, additionalData.de24);
            iso.PutField(48, additionalData.de48);
            iso.PutField(56, additionalData.de56);
            iso.PutField(60, additionalData.de60);
            iso.PutField(61, additionalData.de61);
            iso.PutField(62, additionalData.de62);
            iso.PutField(63, additionalData.de63);
            iso.PutField(64, additionalData.de64);
            iso.PutField(65, additionalData.de65);

            //copy additional data element
            iso.PutField(127, NbMessage.ContructXmlMessage(de_127));

            //check for key exchange
            if (rsp.tran_type == TranType.KEYCHANGE)
            {
                iso.PutField(48, rsp.security.miscdata);

                if (rsp.msgtype == null) iso.MsgType = "0810";
            }

            return iso;
        }

        public static string GetTranTypeStr(string msgtype, string pcode)
        {
            string ret = "";

            switch (msgtype)
            {
                #region Advice
                case "0120":
                case "0121":
                case "0130":
                case "0131":
                case "0220":
                case "0221":
                case "0230":
                case "0231":
                    ret = TranType.ADVICE;
                    break;
                #endregion

                #region Reversal
                case "0400":
                case "0401":
                case "0410":
                case "0411":
                case "0420":
                case "0421":
                case "0430":
                case "0431":
                    ret = TranType.REVERSAL;
                    break;
                #endregion

                default:
                    ret = GetTranTypeStr(pcode);
                    break;
            }

            return ret;
        }
        public static string GetTranTypeStr(string pcode)
        {
            string ret = "";

            if (string.IsNullOrEmpty(pcode) == false && pcode.Length == 6)
            {
                switch (pcode.Substring(0, 2))
                {
                    #region Withdrawal, Purchase, Refund & Deposit
                    case "00":
                        ret = TranType.PURCHASE;
                        break;

                    case "01":
                    case "02":
                    case "03":
                    case "04":
                    case "05":
                    case "06":
                    case "07":
                    case "08":
                    case "09":
                    case "10":
                    case "11":
                    case "12":
                    case "13":
                    case "14":
                    case "15":
                    case "16":
                    case "17":
                    case "18":
                    case "19":
                        ret = TranType.WITHDRAWAL;
                        break;

                    case "20":
                        ret = TranType.REFUND;
                        break;
                    case "21":
                    case "22":
                    case "23":
                    case "24":
                    case "25":
                    case "26":
                    case "27":
                    case "28":
                    case "29":
                        ret = TranType.DEPOSIT;
                        break;
                    #endregion

                    #region Inquiry, Check Balance, Mini Statement
                    case "30":
                    case "31":
                    case "32":
                    case "33":
                    case "34":
                        ret = TranType.INQBALANCE;
                        break;
                    case "35":
                        ret = TranType.MINISTATEMENT;
                        break;
                    case "36":
                    case "37":
                    case "38":
                    case "39":
                        ret = TranType.INQUIRY;
                        break;
                    #endregion

                    #region Payment & Transfer
                    case "40":
                    case "41":
                    case "42":
                    case "43":
                    case "44":
                    case "45":
                    case "46":
                    case "47":
                    case "48":
                    case "49":
                        ret = TranType.TRANSFER;
                        break;

                    case "50":
                    case "51":
                    case "52":
                    case "53":
                    case "54":
                    case "55":
                    case "56":
                    case "57":
                    case "58":
                    case "59":
                        ret = TranType.PAYMENT;
                        break;
                    #endregion

                    #region Admin, Key Exchange, Pin Change & VA
                    case "90":
                    case "91":
                        ret = TranType.ADMIN;
                        break;
                    case "92":
                        ret = TranType.PINCHANGE;
                        break;
                    case "93":
                        ret = TranType.KEYCHANGE;
                        break;
                    case "94":
                        ret = TranType.VTOPUP;
                        break;
                    case "95":
                        ret = TranType.VADJUST;
                        break;
                    case "96":
                        ret = TranType.VBALANCE;
                        break;
                        #endregion
                }
            }

            return ret;
        }

        private static string GetAmount(decimal amount)
        {
            long amt = (long)amount;

            return amt.ToString().PadLeft(12, '0');
        }
        private static string GetOrigData(string pcode, string origdata)
        {
            if (string.IsNullOrEmpty(origdata) == true) return "";

            //add tran type to orig data
            return GetTranTypeStr(pcode) + origdata;
        }
        private static string GetPCode(string tran_type, string tran_type_ext, string from_acc_type, string to_acc_type)
        {
            string ret = "99";

            switch (tran_type)
            {
                case TranType.INQBALANCE:
                    ret = "31";
                    break;
                case TranType.MINISTATEMENT:
                    ret = "35";
                    break;
                case TranType.INQUIRY:
                    ret = "38";
                    break;

                case TranType.WITHDRAWAL:
                    ret = "01";
                    break;
                case TranType.PAYMENT:
                    ret = "50";
                    break;
                case TranType.PURCHASE:
                    ret = "00";
                    break;
                case TranType.TRANSFER:
                    ret = "41";
                    break;
                case TranType.REFUND:
                    ret = "20";
                    break;
                case TranType.DEPOSIT:
                    ret = "21";
                    break;

                case TranType.ADVICE:
                    ret = "50";
                    break;

                case TranType.REVERSAL:
                    ret = "50";
                    break;

                case TranType.ADMIN:
                    ret = "91";
                    break;
                case TranType.PINCHANGE:
                    ret = "92";
                    break;
                case TranType.KEYCHANGE:
                    ret = "93";
                    break;
                case TranType.VTOPUP:
                    ret = "94";
                    break;
                case TranType.VADJUST:
                    ret = "95";
                    break;
                case TranType.VBALANCE:
                    ret = "96";
                    break;
            }

            //replace with extended tran type
            if (string.IsNullOrEmpty(tran_type_ext) == false && tran_type_ext.Length == 2)
                ret = tran_type_ext;

            return ret + from_acc_type + to_acc_type;
        }
        private static string GetTranType(string pcode)
        {
            string tran_type = "";

            if (string.IsNullOrEmpty(pcode) == false && pcode.Length == 6)
                tran_type = pcode.Substring(0, 2);

            return tran_type;
        }
        private static string GetFromAccType(string pcode)
        {
            string acc_type = "00";

            if (string.IsNullOrEmpty(pcode) == false && pcode.Length == 6)
                acc_type = pcode.Substring(2, 2);

            return acc_type;
        }
        private static string GetToAccType(string pcode)
        {
            string acc_type = "00";

            if (string.IsNullOrEmpty(pcode) == false && pcode.Length == 6)
                acc_type = pcode.Substring(4, 2);

            return acc_type;
        }

        private static AdditionalData GetPrivateDataRequest(Dictionary<string, object> obj)
        {
            var res = new AdditionalData();

            try
            {
                if (obj.ContainsKey(PrivateDataRequest))
                {
                    string json = obj[PrivateDataRequest]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(json))
                    {
                        // deserialize object
                        res = JsonConvert.DeserializeObject<AdditionalData>(json);
                    }
                }
            }
            catch { }

            return res;
        }
        private static AdditionalData GetPrivateDataResponse(Dictionary<string, object> obj)
        {
            var res = new AdditionalData();

            try
            {
                if (obj.ContainsKey(PrivateDataResponse))
                {
                    string json = obj[PrivateDataResponse]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(json))
                    {
                        // deserialize object
                        res = JsonConvert.DeserializeObject<AdditionalData>(json);
                    }
                }
                else
                {
                    // pakai data request
                    res = GetPrivateDataRequest(obj);
                }
            }
            catch { }

            return res;
        }

        public class AdditionalData
        {
            public string de12 { get; set; }
            public string de13 { get; set; }
            public string de15 { get; set; }
            public string de24 { get; set; }
            public string de48 { get; set; }
            public string de56 { get; set; }
            public string de60 { get; set; }
            public string de61 { get; set; }
            public string de62 { get; set; }
            public string de63 { get; set; }
            public string de64 { get; set; }
            public string de65 { get; set; }

            public Dictionary<string, string> de127 { get; set; } = [];
        }
    }
}
