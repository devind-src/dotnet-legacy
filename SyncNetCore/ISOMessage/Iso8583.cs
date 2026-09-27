using SyncNet.Library;
using System;
using System.Net;

namespace SyncNet.ISOMessage
{
    public class Iso8583
    {
        private const string LOG_NAME = "Library.IsoMessage";
        private const int DEFAULT_HEADER_LENGTH = 2;

        private FieldFormatter _f;

        //remove logger because impact to performance
        //private NbLogger _logger;

        private EndPoint _ep;
        private string _nodename = "";
        private string _msgtype = "";
        private string _tpdu = "";
        private string _lastErr = "";
        private string _trace = "";
        private string[] _de;

        public Iso8583()
        {
            //_logger = new NbLogger(LOG_NAME);
            this._f = new FieldFormatter();
            this._de = new string[129];
            this._trace = string.Empty;
        }

        public Iso8583(string msgtype, string[] de)
        {
            //_logger = new NbLogger(LOG_NAME);
            this._f = new FieldFormatter();
            this._de = new string[129];
            this._msgtype = msgtype;

            Array.Copy(de, this._de, de.Length);
        }

        public Iso8583(FieldFormatter f)
        {
            //_logger = new NbLogger(LOG_NAME);
            this._de = new string[129];
            this._f = f;
        }

        public Iso8583(FieldFormatter f, string msgtype, string[] de)
        {
            //_logger = new NbLogger(LOG_NAME);
            this._de = new string[129];
            this._msgtype = msgtype;
            this._f = f;

            Array.Copy(de, this._de, de.Length);
        }

        public Iso8583(FieldFormatter f, Iso8583 IsoMsg)
        {
            //_logger = new NbLogger(LOG_NAME);
            this._ep = IsoMsg._ep;
            this._nodename = IsoMsg._nodename;
            this._tpdu = IsoMsg._tpdu;
            this._msgtype = IsoMsg._msgtype;
            this._de = IsoMsg.GetArray();
            this._f = f;
        }

        public Iso8583(Iso8583 IsoMsg)
        {
            //_logger = new NbLogger(LOG_NAME);
            this._f = new FieldFormatter();
            this._ep = IsoMsg._ep;
            this._nodename = IsoMsg._nodename;
            this._tpdu = IsoMsg._tpdu;
            this._msgtype = IsoMsg._msgtype;
            this._de = IsoMsg.GetArray();
        }

        public EndPoint RemoteEP
        {
            get { return this._ep; }
            set { this._ep = value; }
        }

        public string NodeName
        {
            get { return this._nodename; }
            set { this._nodename = value; }
        }

        public string MsgType
        {
            get { return this._msgtype; }
            set { this._msgtype = value; }
        }

        public string TPDU
        {
            get { return this._tpdu; }
            set { this._tpdu = value; }
        }

        public FieldFormatter GetFieldFormatter()
        {
            return this._f;
        }

        public void PutTpdu(string tpdu)
        {
            this._tpdu = tpdu;
        }

        public string GetTpdu()
        {
            return this._tpdu;
        }

        public void PutMsgType(string msgtype)
        {
            this._msgtype = msgtype;
        }

        public string GetMsgType()
        {
            return this._msgtype;
        }

        public void PutField(int field_nr, string value)
        {
            this._de[field_nr] = value;
        }

        public string GetField(int field_nr)
        {
            return this._de[field_nr];
        }

        public string GetLastError()
        {
            return this._lastErr;
        }

        public string[] GetArray()
        {
            string[] tmp = new string[129];
            Array.Copy(this._de, tmp, this._de.Length);
            return tmp;
        }

        public string GetMessage()
        {
            string[] tmp = new string[129];
            string msg = "";
            string lVar;
            int n;

            //copy array to tmp            
            this._de.CopyTo(tmp, 0);

            for (int i = 2; i < tmp.Length; i++)
            {
                if (string.IsNullOrEmpty(tmp[i]) == false)
                {
                    if (this._f.GetFieldType(i) == Field.EnumFieldType.BCD)
                        tmp[i] = NbConvert.ToBCD(tmp[i]);

                    if (this._f.GetFieldFormat(i) != Field.EnumFieldFormat.Fixed)
                    {
                        if (this._f.LenVarFormat == FieldFormatter.EnumMsgFormat.ASC)
                            tmp[i] = NbSystem.SetlVar(tmp[i], this._f.GetLengthVar(i));
                        else
                        {
                            n = this._f.GetLengthVar(i);

                            if (n % 2 != 0)
                                n = n + 1;

                            if (this._f.GetFieldType(i) == Field.EnumFieldType.BCD)
                                //length var is BCD & data is BCD
                                lVar = (tmp[i].Length * 2).ToString().PadLeft(n, '0');
                            else
                                //length var is BCD & data is ASC              
                                lVar = tmp[i].Length.ToString().PadLeft(n, '0');

                            lVar = NbConvert.ToBCD(lVar);

                            tmp[i] = lVar + tmp[i];
                        }
                    }

                    msg = msg + tmp[i];
                }
            }

            string ret = "";

            if (this._f.MsgTypeFormat == FieldFormatter.EnumMsgFormat.BCD)
                ret = NbConvert.HexToString(this._tpdu) + NbConvert.ToBCD(this._msgtype) + GetBitmap() + msg;
            else
                ret = NbConvert.HexToString(this._tpdu) + this._msgtype + GetBitmap() + msg;

            return ret;
        }

        public void CopyFromArray(string[] de)
        {
            de.CopyTo(this._de, 0);
        }

        public byte[] Pack()
        {
            return Pack(NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader);
        }

        public byte[] Pack(string msgtype)
        {
            this._msgtype = msgtype;
            return Pack(NbMessage.EnumTCPHeader.TCP_2_Bytes_ASC_ExcludeHeader);
        }

        public byte[] Pack(NbMessage.EnumTCPHeader tcpheader)
        {
            byte[] bytes;

            if (tcpheader == NbMessage.EnumTCPHeader.None)
                bytes = NbConvert.StringToBytes(GetMessage());
            else
                bytes = NbMessage.AddTCPHeader(GetMessage(), tcpheader);

            return bytes;
        }

        public byte[] Pack(char trailer)
        {
            byte[] bytes = NbConvert.StringToBytes(GetMessage() + trailer);

            return bytes;
        }

        public byte[] Pack(NbMessage.EnumTCPHeader tcpheader, string msgtype)
        {
            this._msgtype = msgtype;
            return Pack(tcpheader);
        }

        public byte[] Pack(NbMessage.EnumTCPHeader tcpheader, string tpdu, string msgtype)
        {
            this._tpdu = tpdu;
            this._msgtype = msgtype;
            return Pack(tcpheader);
        }

        public int Unpack(byte[] bytes)
        {
            return Unpack(bytes, DEFAULT_HEADER_LENGTH);
        }

        public int Unpack(byte[] bytes, int headerlength, int tpdulength)
        {
            //there is tpdu header
            byte[] tmp = new byte[tpdulength];
            Array.Copy(bytes, headerlength, tmp, 0, tpdulength);
            this._tpdu = NbConvert.BytesToHex(tmp);

            return Unpack(bytes, headerlength + tpdulength);
        }

        public int Unpack(byte[] bytes, int headerlength)
        {
            //remove tcp header
            if (headerlength > 0)
                bytes = NbMessage.RemoveTCPHeader(bytes, (byte)headerlength);

            //convert to string
            string msg = NbConvert.BytesToString(bytes);

            return Unpack(msg);
        }

        public int Unpack(string msg)
        {
            int fieldnumber = 0;
            int ret = 0;

            try
            {
                //clear data element
                for (int i = 0; i < this._de.Length; i++)
                    this._de[i] = "";

                string sVar = string.Empty;
                int iPos;
                int lVar;

                //get msgtype
                if (this._f.MsgTypeFormat == FieldFormatter.EnumMsgFormat.BCD)
                    this._msgtype = NbConvert.FromBCD(msg.Substring(0, 2));
                else
                    this._msgtype = msg.Substring(0, 4);

                //get primary bitmap
                string bitmap = "";
                if (this._f.MsgTypeFormat == FieldFormatter.EnumMsgFormat.BCD &&
                    this._f.BitmapFormat == FieldFormatter.EnumMsgFormat.BCD)
                {
                    bitmap = NbConvert.FromBCD(msg.Substring(2, 8));
                    if (NbMath.HexToBin(bitmap.Substring(0, 1)).Substring(0, 1) == "1")
                        bitmap = NbConvert.FromBCD(msg.Substring(2, 16));
                }
                else if (this._f.MsgTypeFormat == FieldFormatter.EnumMsgFormat.ASC &&
                    this._f.BitmapFormat == FieldFormatter.EnumMsgFormat.BCD)
                {
                    bitmap = NbConvert.FromBCD(msg.Substring(4, 8));
                    if (NbMath.HexToBin(bitmap.Substring(0, 1)).Substring(0, 1) == "1")
                        bitmap = NbConvert.FromBCD(msg.Substring(4, 16));
                }
                else
                {
                    bitmap = msg.Substring(4, 16);
                    if (NbMath.HexToBin(bitmap.Substring(0, 1)).Substring(0, 1) == "1")
                        bitmap = msg.Substring(4, 32);
                }

                //convert to binary
                string binner = NbMath.HexToBin(bitmap);
                byte[] arrayBinner = new byte[129];
                for (int i = 1; i <= binner.Length; i++)
                    arrayBinner[i] = (byte)(NbConvert.ToInt(binner.Substring(i - 1, 1)));

                //fill value data element
                this._de[0] = bitmap.Substring(0, 16);

                //set offset data
                if (this._f.MsgTypeFormat == FieldFormatter.EnumMsgFormat.BCD &&
                    this._f.BitmapFormat == FieldFormatter.EnumMsgFormat.BCD)
                    iPos = 11; //msgtype(2) + bitmap(8)
                else if (this._f.MsgTypeFormat == FieldFormatter.EnumMsgFormat.ASC &&
                    this._f.BitmapFormat == FieldFormatter.EnumMsgFormat.BCD)
                    iPos = 13; //msgtype(4) + bitmap(8)
                else
                    iPos = 21; //msgtype(4) + bitmap(16)

                for (fieldnumber = 1; fieldnumber <= 128; fieldnumber++)
                {
                    if (arrayBinner[fieldnumber] == 1)
                    {
                        if (this._f.GetFieldFormat(fieldnumber) == Field.EnumFieldFormat.Fixed)
                        {
                            if (_f.GetFieldType(fieldnumber) == Field.EnumFieldType.ASCII)
                            {
                                this._de[fieldnumber] = msg.Substring(iPos - 1, this._f.GetFieldLength(fieldnumber));
                                iPos = iPos + this._de[fieldnumber].Length;
                            }
                            else //BCD
                            {
                                //format data is BCD
                                int iLen = _f.GetFieldLength(fieldnumber);
                                if (iLen % 2 == 1)
                                    iLen++;

                                iLen = iLen / 2;

                                this._de[fieldnumber] = NbConvert.FromBCD(msg.Substring(iPos - 1, iLen));
                                iPos = iPos + iLen;
                            }
                        }
                        else //LVAR
                        {
                            if (_f.LenVarFormat == FieldFormatter.EnumMsgFormat.ASC)
                            {
                                //validate LVAR
                                sVar = msg.Substring(iPos - 1, this._f.GetLengthVar(fieldnumber));
                                if (NbSystem.IsNumeric(sVar) == false)
                                {
                                    _lastErr = "Unable to unpack field " + fieldnumber + ", length variable invalid";
                                    return -2;
                                }

                                lVar = NbConvert.ToInt(sVar);

                                this._de[fieldnumber] = msg.Substring(iPos + this._f.GetLengthVar(fieldnumber) - 1, lVar);
                                iPos = iPos + this._de[fieldnumber].Length + this._f.GetLengthVar(fieldnumber);
                            }
                            else //BCD
                            {
                                //length variable is BCD
                                int lenVar = _f.GetLengthVar(fieldnumber);

                                if (lenVar > 1)
                                {
                                    if (lenVar % 2 == 0)
                                        lenVar = lenVar / 2;
                                    else
                                        lenVar = (lenVar + 1) / 2;
                                }

                                //validate LVAR
                                sVar = NbConvert.FromBCD(msg.Substring(iPos - 1, lenVar));
                                if (NbSystem.IsNumeric(sVar) == false)
                                {
                                    _lastErr = "Unable to unpack field " + fieldnumber + ", length variable invalid";
                                    return -2;
                                }

                                //this value will be change for BCD
                                lVar = int.Parse(sVar);

                                //save original value
                                int lVarCopy = lVar;

                                //format data is BCD
                                if (_f.GetFieldType(fieldnumber) == Field.EnumFieldType.BCD)
                                {
                                    if (lVar % 2 == 0)
                                        lVar = lVar / 2;
                                    else
                                        lVar = Convert.ToInt32(lVar / 2) + 1;

                                    this._de[fieldnumber] = NbConvert.FromBCD(msg.Substring(iPos + lenVar - 1, lVar));
                                    iPos = iPos + lenVar + lVar;

                                    //remove trailer if exist
                                    this._de[fieldnumber] = this._de[fieldnumber].Substring(0, lVarCopy);
                                }
                                else
                                {
                                    this._de[fieldnumber] = msg.Substring(iPos + lenVar - 1, lVar);
                                    iPos = iPos + this._de[fieldnumber].Length + lenVar;
                                }
                            }
                        }
                    }
                }

                //validate message
                if (IsValidMessage() == false)
                    return -1;
            }
            catch //(Exception ex)
            {
                ret = -3;

                _lastErr = "Unable to unpack message " + this._msgtype + " data element " + fieldnumber.ToString();

                //_logger.Log(_lastErr + Environment.NewLine + ex.Message, ex.StackTrace);                
            }

            return ret;
        }

        public string GetTrace(byte[] bytes)
        {
            return GetTrace(bytes, DEFAULT_HEADER_LENGTH);
        }

        public string GetTrace(byte[] bytes, int headerlength)
        {
            this._trace = NbMessage.FormatBinary(bytes);

            //unpack the message
            Unpack(bytes, headerlength);

            return GetTrace();
        }

        public string GetTrace()
        {
            return GetTrace(DEFAULT_HEADER_LENGTH);
        }

        public string GetTrace(int headerlength)
        {
            string ret = GetFormattedMessage();

            if (string.IsNullOrEmpty(this._trace) == false)
            {
                ret = ret + Environment.NewLine + this._trace;
            }
            else
            {
                if (headerlength == 0)
                    ret = ret +
                        Environment.NewLine +
                        NbMessage.FormatBinary(Pack(NbMessage.EnumTCPHeader.None));
                else
                    ret = ret +
                        Environment.NewLine +
                        NbMessage.FormatBinary(Pack());
            }

            return ret;
        }

        public string GetBinary(byte[] bytes)
        {
            return GetFormattedMessage() + Environment.NewLine + NbMessage.FormatBinary(bytes);
        }

        public string GetFormattedMessage()
        {
            string[] tmp = new string[129];

            //backup array
            this._de.CopyTo(tmp, 0);

            //check whether bitmap has been constructed
            if (string.IsNullOrEmpty(this._de[0]) == true)
                GetBitmap();

            string ret = this._msgtype + Environment.NewLine;
            string err = "";

            for (int i = 0; i < this._de.Length; i++)
            {
                if (string.IsNullOrEmpty(this._de[i]) == false)
                {
                    err = IsValidField(i, this._de[i]);

                    if (err == "OK")
                    {
                        ret = ret +
                            "[" +
                            this._f.GetFieldFormat(i).ToString().PadRight(9, ' ') +
                            this._f.GetFieldAttribute(i).ToString().PadRight(5, ' ') +
                            this._f.GetFieldLength(i).ToString().PadLeft(6, ' ') +
                            " " +
                            this._de[i].Length.ToString().PadLeft(3, '0') +
                            "] " +
                            i.ToString().PadLeft(3, '0') +
                            "  [" +
                            NbMessage.FormatString(this._de[i]) +
                            "]" +
                            Environment.NewLine;
                    }
                    else
                    {
                        ret = ret +
                            Environment.NewLine +
                            err;
                        break;
                    }
                }
            }

            //restore backup array
            tmp.CopyTo(this._de, 0);

            ret = ret + Environment.NewLine;

            return ret;
        }

        public bool IsRequestMessage()
        {
            bool bval = false;

            if (string.IsNullOrEmpty(this._msgtype) == false)
            {
                if (this._msgtype.Substring(2, 1) == "0" ||
                    this._msgtype.Substring(2, 1) == "2")
                {
                    bval = true;
                }
            }

            return bval;
        }

        public bool IsResponseMessage()
        {
            bool bval = false;

            if (string.IsNullOrEmpty(this._msgtype) == false)
            {
                if (this._msgtype.Substring(2, 1) == "1" ||
                    this._msgtype.Substring(2, 1) == "3")
                {
                    bval = true;
                }
            }

            return bval;
        }


        private bool IsValidMessage()
        {
            bool ret = true;
            int iLen;

            for (int i = 0; i <= 128; i++)
            {
                if (string.IsNullOrEmpty(this._de[i]) == false)
                {
                    if (this._f.GetFieldType(i) == Field.EnumFieldType.BCD)
                    {
                        iLen = this._f.GetFieldLength(i);
                        if (iLen % 2 == 1)
                            iLen++;
                    }
                    else
                        iLen = this._f.GetFieldLength(i);

                    if (this._de[i].Length > iLen)
                    {
                        this._lastErr = "Length data element " + i.ToString() + " is more than " + this._f.GetFieldLength(i);
                        //_logger.Log(this._lastErr + Environment.NewLine + getTrace());
                        ret = false;
                        break;
                    }
                    else if (this._f.GetFieldAttribute(i) == Field.EnumFieldAtribute.n && NbSystem.IsNumeric(this._de[i]) == false)
                    {
                        //add this logic to support PC DSS
                        if (i == 2 && this._de[i].Contains("*") == true)
                            ret = true;
                        else
                        {
                            this._lastErr = "Data element " + i.ToString() + " should be numeric.";
                            //_logger.Log(this._lastErr + Environment.NewLine + getTrace());
                            ret = false;
                            break;
                        }
                    }
                }
            }

            return ret;
        }

        private string IsValidField(int FieldNumber, string Value)
        {
            string ret = "OK";
            int iLen;

            if (string.IsNullOrEmpty(Value) == false)
            {
                if (this._f.GetFieldType(FieldNumber) == Field.EnumFieldType.BCD)
                {
                    iLen = this._f.GetFieldLength(FieldNumber);
                    if (iLen % 2 == 1)
                        iLen++;
                }
                else
                    iLen = this._f.GetFieldLength(FieldNumber);

                if (Value.Length > iLen)
                {
                    ret = "Unable to unpack the message because the length of data element " + FieldNumber + " is more than " + _f.GetFieldLength(FieldNumber).ToString() + ".";
                }
                else if (this._f.GetFieldAttribute(FieldNumber) == Field.EnumFieldAtribute.n && NbSystem.IsNumeric(Value) == false)
                {
                    //add this logic to support PC DSS
                    if ((FieldNumber == 2 || FieldNumber == 35) && Value.Contains("*") == true)
                        ret = "OK";
                    else
                        ret = "Unable to unpack the message, the data element " + FieldNumber + " should be numeric.";
                }
            }

            return ret;
        }

        private string GetBitmap()
        {
            //create secondary bitmap
            string bin = "";
            for (int i = 65; i <= 128; i++)
            {
                if (string.IsNullOrEmpty(this._de[i]) == true)
                    bin = bin + "0";
                else
                    bin = bin + "1";
            }

            string bitmap2nd = NbMath.BinToHex(bin);
            if (bitmap2nd == "0000000000000000")
                this._de[1] = "";
            else
                this._de[1] = bitmap2nd;

            //create primary bitmap
            bin = ""; //clear data
            for (int i = 1; i <= 64; i++)
            {
                if (string.IsNullOrEmpty(this._de[i]) == true)
                    bin = bin + "0";
                else
                    bin = bin + "1";
            }

            string bitmap1st = NbMath.BinToHex(bin);
            this._de[0] = bitmap1st;

            string bitmap = "";
            if (bitmap2nd == "0000000000000000")
                bitmap = bitmap1st;
            else
                bitmap = bitmap1st + bitmap2nd;

            if (this._f.BitmapFormat == FieldFormatter.EnumMsgFormat.BCD)
                bitmap = NbConvert.ToBCD(bitmap);

            return bitmap;
        }
    }
}